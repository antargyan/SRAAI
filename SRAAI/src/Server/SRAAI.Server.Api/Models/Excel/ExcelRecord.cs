namespace SRAAI.Server.Api.Models.Excel;

public enum ExcelChangeType
{
	Inserted = 1,
	Updated = 2,
	Deleted = 3
}

public class ExcelRecord
{
	public Guid Id { get; set; }

	public required string DatasetName { get; set; }

	public required string BusinessKey { get; set; }

	public int VersionNo { get; set; }

	public ExcelChangeType ChangeType { get; set; }

	public string? DataJson { get; set; }

	public DateTimeOffset CreatedAt { get; set; }
}


