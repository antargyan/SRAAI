namespace SRAAI.Server.Api.Controllers.Excel;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class ExcelController : AppControllerBase
{
	[AutoInject] private Services.Excel.IExcelImportService excelImportService = default!;

	[HttpPost]
	[RequestSizeLimit(50 * 1024 * 1024)]
	public async Task<IActionResult> Import([FromQuery] string dataset, IFormFile file, CancellationToken cancellationToken)
	{
		if (file is null || file.Length == 0) throw new BadRequestException();
		await using var stream = file.OpenReadStream();
		var result = await excelImportService.ImportAndCompare(dataset, stream, cancellationToken);
		return Ok(result);
	}

	[HttpGet]
	public async Task<IActionResult> History([FromQuery] string dataset, CancellationToken cancellationToken)
	{
		var sessions = await DbContext.ExcelImportSessions
			.Where(s => s.DatasetName == dataset)
			.OrderByDescending(s => s.NewVersionNo)
			.ToListAsync(cancellationToken);
		return Ok(sessions);
	}

	[HttpGet]
	public async Task<IActionResult> Changes([FromQuery] string dataset, [FromQuery] int version, CancellationToken cancellationToken)
	{
		var rows = await DbContext.ExcelRecords
			.Where(r => r.DatasetName == dataset && r.VersionNo == version)
			.OrderBy(r => r.BusinessKey)
			.ToListAsync(cancellationToken);
		return Ok(rows);
	}
}


