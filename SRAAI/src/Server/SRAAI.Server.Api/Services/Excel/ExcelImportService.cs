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
		if (string.IsNullOrWhiteSpace(datasetName))
			throw new ArgumentException("Dataset name cannot be null or empty.", nameof(datasetName));

		if (excelStream is null || excelStream.Length == 0)
			throw new ArgumentException("Excel stream cannot be null or empty.", nameof(excelStream));

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

		// Parse current excel with error handling
		IEnumerable<(string businessKey, string json)> rows;
		try
		{
			rows = ParseRows(excelStream);
		}
		catch (InvalidOperationException ex)
		{
			throw new InvalidOperationException($"Failed to parse Excel file: {ex.Message}", ex);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException("An error occurred while processing the Excel file. Please ensure the file is a valid Excel (.xlsx) format.", ex);
		}

		var inserted = 0; var updated = 0; var deleted = 0;
		var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var duplicateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var row in rows)
		{
			var businessKey = row.businessKey;
			
			// Check for duplicate keys within the current import
			if (seenKeys.Contains(businessKey))
			{
				duplicateKeys.Add(businessKey);
				continue; // Skip duplicate entries
			}
			
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

		// Validate import results
		if (duplicateKeys.Count > 0)
		{
			var duplicateKeysList = string.Join(", ", duplicateKeys.Take(10));
			var message = duplicateKeys.Count > 10 
				? $"Found {duplicateKeys.Count} duplicate primary keys in email column (showing first 10: {duplicateKeysList}...)"
				: $"Found duplicate primary keys in email column: {duplicateKeysList}";
			throw new InvalidOperationException(message);
		}

		if (inserted == 0 && updated == 0 && lastSnapshot.Count == 0)
		{
			throw new InvalidOperationException("No valid data found in the Excel file. Please ensure email column contains valid primary key values and the file has data rows.");
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

	public static IEnumerable<(string businessKey, string json)> ParseRows(Stream excelStream)
	{
		using var workbook = new ClosedXML.Excel.XLWorkbook(excelStream);
		var ws = workbook.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("Excel file has no worksheets");
		
		// Find header row by locating first non-empty row
		var firstRow = ws.FirstRowUsed() ?? ws.Row(1);
		var headers = firstRow.Cells().Select(c => c.GetString().Trim()).ToArray();
		
		if (headers.Length == 0)
			throw new InvalidOperationException("Excel file has no header row or the header row is empty");
		
		if (headers.Length < 2)
			throw new InvalidOperationException("Excel file must have at least 2 columns. Email column is required as the primary key.");
		
		int startRow = firstRow.RowNumber() + 1;
		var lastRowNumber = ws.LastRowUsed()?.RowNumber() ?? (startRow - 1);
		
		if (lastRowNumber < startRow)
			throw new InvalidOperationException("Excel file has no data rows below the header");
		
		var processedRows = 0;
		var skippedRows = 0;
		var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var duplicateEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		
		for (int r = startRow; r <= lastRowNumber; r++)
		{
			var row = ws.Row(r);
			if (row.IsEmpty()) 
			{
				skippedRows++;
				continue;
			}
			
			var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < headers.Length; i++)
			{
				var cell = row.Cell(i + 1);
				var value = cell.GetFormattedString();
				dict[headers[i]] = string.IsNullOrEmpty(value) ? null : value;
			}
			
			// Use Email column (typically column B) as primary key
			var emailValue = row.Cell(2).GetFormattedString();
			if (string.IsNullOrWhiteSpace(emailValue))
			{
				skippedRows++;
				continue; // Skip rows with empty email values
			}
			
			// Validate that the email is not just whitespace
			emailValue = emailValue.Trim();
			if (string.IsNullOrEmpty(emailValue))
			{
				skippedRows++;
				continue;
			}
			
			// Check for duplicate emails within the current import
			if (seenEmails.Contains(emailValue))
			{
				duplicateEmails.Add(emailValue);
				skippedRows++;
				continue; // Skip duplicate email entries
			}
			seenEmails.Add(emailValue);
			
			// Validate that Name column (typically column A) is not empty
			var nameValue = row.Cell(1).GetFormattedString();
			if (string.IsNullOrWhiteSpace(nameValue))
			{
				skippedRows++;
				continue; // Skip rows with empty name values
			}
			
			// Add the primary key to the dictionary for consistency
			dict["BusinessKey"] = emailValue;
			
			var json = System.Text.Json.JsonSerializer.Serialize(dict);
			processedRows++;
			yield return (emailValue, json);
		}
		
		// Validate that we processed at least some data
		if (processedRows == 0)
		{
			var message = skippedRows > 0 
				? $"No valid data rows found. All {skippedRows} data rows were skipped because email or name was empty, or duplicate emails were found."
				: "No data rows found in the Excel file.";
			throw new InvalidOperationException(message);
		}
		
		// Log information about duplicates if any were found
		if (duplicateEmails.Count > 0)
		{
			// Note: In a real application, you might want to log this information
			// For now, we silently skip duplicates as requested
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


