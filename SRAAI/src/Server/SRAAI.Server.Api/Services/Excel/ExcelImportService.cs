namespace SRAAI.Server.Api.Services.Excel;

public record ExcelImportResult(int NewVersionNo, int Inserted, int Updated, int Deleted, string? AiSummaryEn, string? AiSummaryMr, Guid SessionId);

public interface IExcelImportService
{
	Task<ExcelImportResult> ImportAndCompare(string datasetName, Stream excelStream, CancellationToken cancellationToken);
}

public partial class ExcelImportService : IExcelImportService
{
	[AutoInject] private AppDbContext dbContext = default!;
	[AutoInject] private IServiceProvider serviceProvider = default!;
	[AutoInject] private IConfiguration configuration = default!;

	public async Task<ExcelImportResult> ImportAndCompare(string datasetName, Stream excelStream, CancellationToken cancellationToken)
	{
		// Determine last version
		int lastVersion = await dbContext.ExcelRecords
			.Where(r => r.DatasetName == datasetName)
			.Select(r => (int?)r.VersionNo)
			.MaxAsync(cancellationToken) ?? 0;

		var newVersion = lastVersion + 1;

		// Load prior state by business key -> json
		var lastSnapshot = await dbContext.ExcelRecords
			.Where(r => r.DatasetName == datasetName && r.VersionNo == lastVersion)
			.ToDictionaryAsync(r => r.BusinessKey, r => r.DataJson, cancellationToken);

		// Parse current excel
		var rows = ParseRows(excelStream);
		var inserted = 0; var updated = 0; var deleted = 0;
		var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var row in rows)
		{
			var businessKey = row.businessKey;
			seenKeys.Add(businessKey);
			var currentJson = row.json;
			lastSnapshot.TryGetValue(businessKey, out var previousJson);

			if (previousJson is null)
			{
				inserted++;
				await dbContext.ExcelRecords.AddAsync(new Models.Excel.ExcelRecord
				{
					DatasetName = datasetName,
					BusinessKey = businessKey,
					VersionNo = newVersion,
					ChangeType = Models.Excel.ExcelChangeType.Inserted,
					DataJson = currentJson
				}, cancellationToken);
			}
			else if (string.Equals(previousJson, currentJson, StringComparison.Ordinal))
			{
				// unchanged: store as Updated=0? We skip storing unchanged rows to keep history concise
			}
			else
			{
				updated++;
				await dbContext.ExcelRecords.AddAsync(new Models.Excel.ExcelRecord
				{
					DatasetName = datasetName,
					BusinessKey = businessKey,
					VersionNo = newVersion,
					ChangeType = Models.Excel.ExcelChangeType.Updated,
					DataJson = currentJson
				}, cancellationToken);
			}
		}

		// deletions (keys in lastSnapshot not present now)
		foreach (var kvp in lastSnapshot)
		{
			if (seenKeys.Contains(kvp.Key)) continue;
			deleted++;
			await dbContext.ExcelRecords.AddAsync(new Models.Excel.ExcelRecord
			{
				DatasetName = datasetName,
				BusinessKey = kvp.Key,
				VersionNo = newVersion,
				ChangeType = Models.Excel.ExcelChangeType.Deleted,
				DataJson = null
			}, cancellationToken);
		}

		// AI summaries
		(string? en, string? mr) summaries = await GenerateSummaries(datasetName, newVersion, inserted, updated, deleted, cancellationToken);

		var session = new Models.Excel.ExcelImportSession
		{
			DatasetName = datasetName,
			NewVersionNo = newVersion,
			PreviousVersionNo = lastVersion,
			InsertedCount = inserted,
			UpdatedCount = updated,
			DeletedCount = deleted,
			AiSummaryEn = summaries.en,
			AiSummaryMr = summaries.mr
		};
		await dbContext.ExcelImportSessions.AddAsync(session, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);

		return new ExcelImportResult(newVersion, inserted, updated, deleted, session.AiSummaryEn, session.AiSummaryMr, session.Id);
	}

	private static IEnumerable<(string businessKey, string json)> ParseRows(Stream excelStream)
	{
		using var workbook = new ClosedXML.Excel.XLWorkbook(excelStream);
		var ws = workbook.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("Excel has no worksheets");
		// Find header row by locating first non-empty row
		var firstRow = ws.FirstRowUsed() ?? ws.Row(1);
		var headers = firstRow.Cells().Select(c => c.GetString().Trim()).ToArray();
		if (headers.Length == 0) yield break;
		int startRow = firstRow.RowNumber() + 1;
		var lastRowNumber = ws.LastRowUsed()?.RowNumber() ?? (startRow - 1);
		for (int r = startRow; r <= lastRowNumber; r++)
		{
			var row = ws.Row(r);
			if (row.IsEmpty()) continue;
			var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < headers.Length; i++)
			{
				var cell = row.Cell(i + 1);
				var value = cell.GetFormattedString();
				dict[headers[i]] = string.IsNullOrEmpty(value) ? null : value;
			}
			// Require a BusinessKey column
			if (dict.TryGetValue("BusinessKey", out var keyObj) is false || string.IsNullOrWhiteSpace(keyObj?.ToString())) continue;
			var json = System.Text.Json.JsonSerializer.Serialize(dict);
			yield return (keyObj!.ToString()!, json);
		}
	}

	private async Task<(string? en, string? mr)> GenerateSummaries(string datasetName, int newVersion, int inserted, int updated, int deleted, CancellationToken cancellationToken)
	{
		var chatClient = serviceProvider.GetService<IChatClient>();
		if (chatClient is null)
			return (null, null);

		ChatOptions chatOptions = new();
		configuration.GetRequiredSection("AI:ChatOptions").Bind(chatOptions);

		var system = new ChatMessage(ChatRole.System, "You are a helpful release notes writer. Output short bullet summary.");
		var user = new ChatMessage(ChatRole.User, $"Dataset: {datasetName}. New version: {newVersion}. Inserted: {inserted}. Updated: {updated}. Deleted: {deleted}.\nReturn two sections: English and Marathi.");

		var response = await chatClient.GetResponseAsync([system, user], options: chatOptions, cancellationToken: cancellationToken);
		var text = response?.Text;
		if (string.IsNullOrWhiteSpace(text))
			return (null, null);

		// Simple split heuristic. If not found, duplicate in both fields.
		string? en = null, mr = null;
		var split = text.Split("Marathi", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (split.Length >= 2)
		{
			en = split[0];
			mr = split[1];
		}
		else
		{
			en = text;
			mr = text;
		}
		return (en, mr);
	}
}


