namespace SRAAI.Server.Api.Models.Excel;

public class ExcelImportSession
{
	public Guid Id { get; set; }

	public required string DatasetName { get; set; }

	public int NewVersionNo { get; set; }

	public int PreviousVersionNo { get; set; }

	public int InsertedCount { get; set; }

	public int UpdatedCount { get; set; }

	public int DeletedCount { get; set; }

	public string? AiSummaryEn { get; set; }

	public string? AiSummaryMr { get; set; }

	public DateTimeOffset CreatedAt { get; set; }
}


