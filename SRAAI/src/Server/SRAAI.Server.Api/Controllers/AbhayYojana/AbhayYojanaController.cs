using System.Data;
using SRAAI.Server.Api.Services.AbhayYojana;
using SRAAI.Shared.Dtos.AbhayYojana;
using SRAAI.Shared.Dtos.Summary;
using Syncfusion.XlsIO;

namespace SRAAI.Server.Api.Controllers.AbhayYojana;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class AbhayYojanaController : AppControllerBase
{
    [AutoInject] private IAbhayYojanaExcelImportService excelImportService = default!;

    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> ImportExcel(IFormFile file, CancellationToken cancellationToken = default)
     {
        try
        {
            if (file is null || file.Length == 0)
                throw new BadRequestException("No file provided");

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException("Only Excel (.xlsx) files are supported");

            if (file.Length > 50 * 1024 * 1024)
                throw new BadRequestException("File size exceeds 50MB limit");

            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Xlsx;

                using (var inputStream = file.OpenReadStream())
                {
                    IWorkbook workbook = application.Workbooks.Open(inputStream);

                    // Get first worksheet
                    IWorksheet sheet1 = workbook.Worksheets[0];
                    // Get second worksheet (assuming it exists)
                    //IWorksheet sheet2 = workbook.Worksheets.Count > 1 ? workbook.Worksheets[1] : null;

                    /* if (sheet2 == null)
                         throw new BadRequestException("Excel file must have at least 2 sheets.");
 */
                    int headerRow = 4;
                    int firstCol = sheet1.UsedRange.Column;
                    int lastCol = 13;
                    int lastRow = sheet1.UsedRange.LastRow;

                    DataTable customersTable = sheet1.ExportDataTable(
                        headerRow,
                        firstCol,
                        lastRow,
                        lastCol,
                        ExcelExportDataTableOptions.ColumnNames | ExcelExportDataTableOptions.ComputedFormulaValues);

                    /*   DataTable builderTable = sheet2.ExportDataTable(
                           headerRow,
                           firstCol,
                           lastRow,
                           lastCol,
                           ExcelExportDataTableOptions.ColumnNames | ExcelExportDataTableOptions.ComputedFormulaValues);*/

                    for (int i = 0; i < customersTable.Rows.Count; i += 3)
                    {
                        DataRow row = customersTable.Rows[i];
                        if (string.IsNullOrWhiteSpace(row[0]?.ToString()))
                            break;

                        int originalSlumNumber = Convert.ToInt32(row[1]?.ToString());

                        var presentdata = await DbContext.AbhayYojanaApplications
                            .FirstOrDefaultAsync(a => a.OriginalSlumNumber == originalSlumNumber);

                        if (presentdata == null)
                        {
                         
                            var dto = new AbhayYojanaApplication
                            {
                                OriginalSlumNumber = originalSlumNumber,
                                SerialNumber = Convert.ToInt32(row[0]?.ToString()),
                                OriginalSlumDwellerName = row[2]?.ToString() ?? string.Empty,
                                ApplicantName = row[3]?.ToString() ?? string.Empty,
                                VoterListYear = int.TryParse(row[4]?.ToString(), out int year) ? year : (int?)null,   
                                VoterListPartNumber = row[5]?.ToString(),
                                VoterListSerialNumber = int.TryParse(row[6]?.ToString(), out int serial) ? serial : (int?)null, 
                                VoterListBound = row[7]?.ToString(),
                                SlumUsage = row[8]?.ToString() ?? string.Empty,
                                CarpetAreaSqFt = decimal.TryParse(row[9]?.ToString(), out decimal area) ? area : (decimal?)null,
                                EvidenceDetails = row[10]?.ToString() ?? string.Empty,
                                EligibilityStatus = row[11]?.ToString() ?? string.Empty,
                                Remarks = row[12]?.ToString(),
                                CreatedDate = DateTime.Now,
                            };

                            await DbContext.AbhayYojanaApplications.AddAsync(dto);
                        }
                        else
                        {
                           
                            presentdata.SerialNumber = Convert.ToInt32(row[0]?.ToString());
                            presentdata.OriginalSlumDwellerName = row[2]?.ToString() ?? string.Empty;
                            presentdata.ApplicantName = row[3]?.ToString() ?? string.Empty;
                            presentdata.VoterListYear = int.TryParse(row[4]?.ToString(), out int year) ? year : (int?)null;
                            presentdata.VoterListPartNumber = row[5]?.ToString();
                            presentdata.VoterListSerialNumber = int.TryParse(row[6]?.ToString(), out int serial) ? serial : (int?)null;
                            presentdata.VoterListBound = row[7]?.ToString();
                            presentdata.SlumUsage = row[8]?.ToString() ?? string.Empty;
                            presentdata.CarpetAreaSqFt = decimal.TryParse(row[9]?.ToString(), out decimal area) ? area : (decimal?)null;
                            presentdata.EvidenceDetails = row[10]?.ToString() ?? string.Empty;
                            presentdata.EligibilityStatus = row[11]?.ToString() ?? string.Empty;
                            presentdata.Remarks = row[12]?.ToString();
                            presentdata.UpdatedDate = DateTime.Now;

                      
                        }
                        await DbContext.SaveChangesAsync();
                    }


                    return Ok(new { message = "Excel imported successfully" });
                }
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = "Import failed",
                message = ex.Message,
                details = ex.StackTrace
            });
        }
    }


    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> ImportUpdateExcel(IFormFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            if (file is null || file.Length == 0)
                throw new BadRequestException("No file provided");

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException("Only Excel (.xlsx) files are supported");

            if (file.Length > 50 * 1024 * 1024)
                throw new BadRequestException("File size exceeds 50MB limit");

            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Xlsx;

                using (var inputStream = file.OpenReadStream())
                {
                    IWorkbook workbook = application.Workbooks.Open(inputStream);

                    IWorksheet sheet1 = workbook.Worksheets[0];
                    int headerRow = 4;
                    int firstCol = sheet1.UsedRange.Column;
                    int lastCol = 13;
                    int lastRow = sheet1.UsedRange.LastRow;
                    DataTable customersTable = sheet1.ExportDataTable(
                        headerRow,
                        firstCol,
                        lastRow,
                        lastCol,
                        ExcelExportDataTableOptions.ColumnNames | ExcelExportDataTableOptions.ComputedFormulaValues);
                         var alldata = await DbContext.AbhayYojanaApplications.ToListAsync();
                             DbContext.RemoveRange(alldata);
                             await DbContext.SaveChangesAsync();

                    for (int i = 0; i < customersTable.Rows.Count; i += 3)
                    {
                        DataRow row = customersTable.Rows[i];
                        if (string.IsNullOrWhiteSpace(row[0]?.ToString()))
                            break;

                        int originalSlumNumber = Convert.ToInt32(row[1]?.ToString());

                        var presentdata = await DbContext.AbhayYojanaApplications
                            .FirstOrDefaultAsync(a => a.OriginalSlumNumber == originalSlumNumber);

                        if (presentdata != null)
                        {
                            presentdata.SerialNumber = Convert.ToInt32(row[0]?.ToString());
                            presentdata.OriginalSlumDwellerName = row[2]?.ToString() ?? string.Empty;
                            presentdata.ApplicantName = row[3]?.ToString() ?? string.Empty;
                            presentdata.VoterListYear = int.TryParse(row[4]?.ToString(), out int year) ? year : (int?)null;
                            presentdata.VoterListPartNumber = row[5]?.ToString();
                            presentdata.VoterListSerialNumber = int.TryParse(row[6]?.ToString(), out int serial) ? serial : (int?)null;
                            presentdata.VoterListBound = row[7]?.ToString();
                            presentdata.SlumUsage = row[8]?.ToString() ?? string.Empty;
                            presentdata.CarpetAreaSqFt = decimal.TryParse(row[9]?.ToString(), out decimal area) ? area : (decimal?)null;
                            presentdata.EvidenceDetails = row[10]?.ToString() ?? string.Empty;
                            presentdata.EligibilityStatus = row[11]?.ToString() ?? string.Empty;
                            presentdata.Remarks = row[12]?.ToString();
                            presentdata.UpdatedDate = DateTime.Now;


                        }
                        DbContext.Update(presentdata);
                        await DbContext.SaveChangesAsync();
                    }


                    return Ok(new { message = "Excel imported successfully" });
                }
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = "Import failed",
                message = ex.Message,
                details = ex.StackTrace
            });
        }
    }

    public async Task<List<AbhayYojanaApplicationDto>> BuilderExcelScanning(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            throw new BadRequestException("No file provided");

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("Only Excel (.xlsx) files are supported");

        if (file.Length > 50 * 1024 * 1024)
            throw new BadRequestException("File size exceeds 50MB limit");

        using (ExcelEngine excelEngine = new ExcelEngine())
        {
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            using var inputStream = file.OpenReadStream();
            IWorkbook workbook = application.Workbooks.Open(inputStream);
            IWorksheet sheet1 = workbook.Worksheets[0];

            int headerRow = 4;
            int firstCol = sheet1.UsedRange.Column;
            int lastCol = 13;
            int lastRow = sheet1.UsedRange.LastRow;

            DataTable customersTable = sheet1.ExportDataTable(
                headerRow,
                firstCol,
                lastRow,
                lastCol,
                ExcelExportDataTableOptions.ColumnNames | ExcelExportDataTableOptions.ComputedFormulaValues);

            var dbData = await DbContext.AbhayYojanaApplications.ToListAsync(cancellationToken);

            List<AbhayYojanaApplicationDto> result = new();

         
            for (int i = 0; i < customersTable.Rows.Count; i += 3)
            {
                DataRow row = customersTable.Rows[i];

                if (string.IsNullOrWhiteSpace(row[0]?.ToString())) break;

                int originalSlumNumber = Convert.ToInt32(row[1]?.ToString());

                var dto = new AbhayYojanaApplicationDto
                {
                    OriginalSlumNumber = originalSlumNumber,
                    SerialNumber = Convert.ToInt32(row[0]?.ToString()),
                    OriginalSlumDwellerName = row[2]?.ToString() ?? string.Empty,
                    ApplicantName = row[3]?.ToString() ?? string.Empty,
                    VoterListYear = int.TryParse(row[4]?.ToString(), out int year) ? year : (int?)null,
                    VoterListPartNumber = row[5]?.ToString(),
                    VoterListSerialNumber = int.TryParse(row[6]?.ToString(), out int serial) ? serial : (int?)null,
                    VoterListBound = row[7]?.ToString(),
                    SlumUsage = row[8]?.ToString() ?? string.Empty,
                    CarpetAreaSqFt = decimal.TryParse(row[9]?.ToString(), out decimal area) ? area : (decimal?)null,
                    EvidenceDetails = row[10]?.ToString() ?? string.Empty,
                    EligibilityStatus = row[11]?.ToString() ?? string.Empty,
                    Remarks = row[12]?.ToString(),
                };

                var originaldata = dbData.FirstOrDefault(a => a.OriginalSlumNumber == originalSlumNumber);

                if (originaldata != null)
                {
                    var changed = new List<string>();

                    if (originaldata.SerialNumber != dto.SerialNumber) changed.Add(nameof(dto.SerialNumber));
                    if (originaldata.OriginalSlumDwellerName != dto.OriginalSlumDwellerName) changed.Add(nameof(dto.OriginalSlumDwellerName));
                    if (originaldata.ApplicantName != dto.ApplicantName) changed.Add(nameof(dto.ApplicantName));
                    if (originaldata.SlumUsage != dto.SlumUsage) changed.Add(nameof(dto.SlumUsage));
                    if (originaldata.EvidenceDetails != dto.EvidenceDetails) changed.Add(nameof(dto.EvidenceDetails));
                    if (originaldata.EligibilityStatus != dto.EligibilityStatus) changed.Add(nameof(dto.EligibilityStatus));
                    if (originaldata.Remarks != dto.Remarks) changed.Add(nameof(dto.Remarks));

                    if (changed.Any())
                    {
                        dto.Status = "NotMatching";
                        dto.ChangedFields = changed;
                    }
                    else
                    {
                        dto.Status = "Matching";
                    }
                }
                else
                {
                    dto.Status = "Additional"; 
                }

                result.Add(dto);
            }

            
            var excelSlumNumbers = customersTable.Rows
     .Cast<DataRow>()
     .Select(r => r[1]?.ToString())
     .Where(s => !string.IsNullOrWhiteSpace(s) && int.TryParse(s, out _))
     .Select(s => int.Parse(s!))
     .ToHashSet();


            var notFound = dbData
                .Where(db => !excelSlumNumbers.Contains(db.OriginalSlumNumber))
                .Select(db => new AbhayYojanaApplicationDto
                {
                    OriginalSlumNumber = db.OriginalSlumNumber,
                    SerialNumber = db.SerialNumber,
                    OriginalSlumDwellerName = db.OriginalSlumDwellerName,
                    ApplicantName = db.ApplicantName,
                    VoterListYear = db.VoterListYear,
                    VoterListPartNumber = db.VoterListPartNumber,
                    VoterListSerialNumber = db.VoterListSerialNumber,
                    SlumUsage = db.SlumUsage,
                    CarpetAreaSqFt = db.CarpetAreaSqFt,
                    EvidenceDetails = db.EvidenceDetails,
                    EligibilityStatus = db.EligibilityStatus,
                    Remarks = db.Remarks,
                    Status = "NotFound"
                });

            result.AddRange(notFound);

            return result;
        }
    }



    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = DbContext.AbhayYojanaApplications.AsQueryable();
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var applications = await query
            .OrderBy(a => a.OriginalSlumNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AbhayYojanaApplicationDto
            {
                SerialNumber = a.SerialNumber,
                OriginalSlumNumber = a.OriginalSlumNumber,
                OriginalSlumDwellerName = a.OriginalSlumDwellerName,
                ApplicantName = a.ApplicantName,
                VoterListYear = a.VoterListYear,
                VoterListPartNumber = a.VoterListPartNumber,
                VoterListSerialNumber = a.VoterListSerialNumber,
                VoterListBound = a.VoterListBound,
                SlumUsage = a.SlumUsage,
                CarpetAreaSqFt = a.CarpetAreaSqFt,
                EvidenceDetails = a.EvidenceDetails,
                EligibilityStatus = a.EligibilityStatus,
                Remarks = a.Remarks,
                CreatedDate = a.CreatedDate,
                UpdatedDate = a.UpdatedDate,
                Version = a.Version
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            Data = applications,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

 

  /*  [HttpGet]
    public async Task<List<AbhayYojanaApplicationDto>> GetAbhayYojanaApplication(CancellationToken cancellationToken)
    {
        var data = await DbContext.AbhayYojanaApplications.ToListAsync(cancellationToken);

        return
    }*/


    [HttpGet("{originalSlumNumber}")]
    public async Task<IActionResult> GetBySlumNumber(int originalSlumNumber, CancellationToken cancellationToken = default)
    {
        var application = await DbContext.AbhayYojanaApplications
            .Where(a => a.OriginalSlumNumber == originalSlumNumber)
            .Select(a => new AbhayYojanaApplicationDto
            {
                SerialNumber = a.SerialNumber,
                OriginalSlumNumber = a.OriginalSlumNumber,
                OriginalSlumDwellerName = a.OriginalSlumDwellerName,
                ApplicantName = a.ApplicantName,
                VoterListYear = a.VoterListYear,
                VoterListPartNumber = a.VoterListPartNumber,
                VoterListSerialNumber = a.VoterListSerialNumber,
                VoterListBound = a.VoterListBound,
                SlumUsage = a.SlumUsage,
                CarpetAreaSqFt = a.CarpetAreaSqFt,
                EvidenceDetails = a.EvidenceDetails,
                EligibilityStatus = a.EligibilityStatus,
                Remarks = a.Remarks,
                CreatedDate = a.CreatedDate,
                UpdatedDate = a.UpdatedDate,
                Version = a.Version
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (application == null)
            return NotFound($"Application with slum number {originalSlumNumber} not found");

        return Ok(application);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAbhayYojanaApplicationDto dto, CancellationToken cancellationToken = default)
    {
        // Check if slum number already exists
        var exists = await DbContext.AbhayYojanaApplications
            .AnyAsync(a => a.OriginalSlumNumber == dto.OriginalSlumNumber, cancellationToken);

        if (exists)
            throw new BadRequestException($"Application with slum number {dto.OriginalSlumNumber} already exists");

        var application = new Data.AbhayYojanaApplication
        {
            SerialNumber = dto.SerialNumber,
            OriginalSlumNumber = dto.OriginalSlumNumber,
            OriginalSlumDwellerName = dto.OriginalSlumDwellerName,
            ApplicantName = dto.ApplicantName,
            VoterListYear = dto.VoterListYear,
            VoterListPartNumber = dto.VoterListPartNumber,
            VoterListSerialNumber = dto.VoterListSerialNumber,
            VoterListBound = dto.VoterListBound,
            SlumUsage = dto.SlumUsage,
            CarpetAreaSqFt = dto.CarpetAreaSqFt,
            EvidenceDetails = dto.EvidenceDetails,
            EligibilityStatus = dto.EligibilityStatus,
            Remarks = dto.Remarks,
            Version = dto.Version,
            CreatedDate = DateTime.UtcNow
        };

        await DbContext.AbhayYojanaApplications.AddAsync(application, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetBySlumNumber), new { originalSlumNumber = application.OriginalSlumNumber }, application);
    }

    [HttpPut("{originalSlumNumber}")]
    public async Task<IActionResult> Update(int originalSlumNumber, [FromBody] UpdateAbhayYojanaApplicationDto dto, CancellationToken cancellationToken = default)
    {
        var application = await DbContext.AbhayYojanaApplications
            .FirstOrDefaultAsync(a => a.OriginalSlumNumber == originalSlumNumber, cancellationToken);

        if (application == null)
            return NotFound($"Application with slum number {originalSlumNumber} not found");

        // Update only provided fields
        if (dto.OriginalSlumDwellerName != null)
            application.OriginalSlumDwellerName = dto.OriginalSlumDwellerName;
        
        if (dto.ApplicantName != null)
            application.ApplicantName = dto.ApplicantName;
        
        if (dto.VoterListYear.HasValue)
            application.VoterListYear = dto.VoterListYear;
        
        if (dto.VoterListPartNumber != null)
            application.VoterListPartNumber = dto.VoterListPartNumber;
        
        if (dto.VoterListSerialNumber.HasValue)
            application.VoterListSerialNumber = dto.VoterListSerialNumber;
        
        if (dto.VoterListBound != null)
            application.VoterListBound = dto.VoterListBound;
        
        if (dto.SlumUsage != null)
            application.SlumUsage = dto.SlumUsage;
        
        if (dto.CarpetAreaSqFt.HasValue)
            application.CarpetAreaSqFt = dto.CarpetAreaSqFt;
        
        if (dto.EvidenceDetails != null)
            application.EvidenceDetails = dto.EvidenceDetails;
        
        if (dto.EligibilityStatus != null)
            application.EligibilityStatus = dto.EligibilityStatus;
        
        if (dto.Remarks != null)
            application.Remarks = dto.Remarks;

        application.UpdatedDate = DateTime.UtcNow;

        await DbContext.SaveChangesAsync(cancellationToken);

        return Ok(application);
    }

    [HttpDelete("{originalSlumNumber}")]
    public async Task<IActionResult> Delete(int originalSlumNumber, CancellationToken cancellationToken = default)
    {
        var application = await DbContext.AbhayYojanaApplications
            .FirstOrDefaultAsync(a => a.OriginalSlumNumber == originalSlumNumber, cancellationToken);

        if (application == null)
            return NotFound($"Application with slum number {originalSlumNumber} not found");

        DbContext.AbhayYojanaApplications.Remove(application);
        await DbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken = default)
    {
        var stats = await DbContext.AbhayYojanaApplications
            .GroupBy(a => a.EligibilityStatus)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var totalCount = await DbContext.AbhayYojanaApplications.CountAsync(cancellationToken);

        return Ok(new
        {
            TotalApplications = totalCount,
            StatusBreakdown = stats,
            LastUpdated = await DbContext.AbhayYojanaApplications
                .MaxAsync(a => (DateTime?)a.UpdatedDate ?? a.CreatedDate, cancellationToken)
        });
    }

    [HttpGet]
    public async Task<List<SummaryDto>> Summary(CancellationToken cancellationToken)
    {
        var data = await DbContext.AbhayYojanaApplications.ToListAsync(cancellationToken);

        // Fixed categories you want to show
        var categories = new[] { "पात्र", "अपात्र", "अनिर्णित" };
        var result = new List<SummaryDto>();

        foreach (var cat in categories)
        {
            var rows = data.Where(x =>
            {
                if (string.IsNullOrWhiteSpace(x.EligibilityStatus))
                    return false;

                var status = x.EligibilityStatus.Trim();

                // Normalize DB values
                if (status == "पात्र नाही")
                    status = "अपात्र";

                return status.Equals(cat, StringComparison.OrdinalIgnoreCase);
            });

            result.Add(new SummaryDto
            {
                Category = cat,
                Nivasi = rows.Count(x => !string.IsNullOrWhiteSpace(x.SlumUsage) &&
                                         x.SlumUsage.Trim().Equals("निवासी", StringComparison.OrdinalIgnoreCase)),
                Anivasi = rows.Count(x => !string.IsNullOrWhiteSpace(x.SlumUsage) &&
                                          x.SlumUsage.Trim().Equals("अनिवासी", StringComparison.OrdinalIgnoreCase)),
                Samyukt = rows.Count(x => !string.IsNullOrWhiteSpace(x.SlumUsage) &&
                                          x.SlumUsage.Trim().Equals("संयुक्त", StringComparison.OrdinalIgnoreCase)),
                Dharsthal = rows.Count(x => !string.IsNullOrWhiteSpace(x.SlumUsage) &&
                                            x.SlumUsage.Trim().Equals("धारस्थळ", StringComparison.OrdinalIgnoreCase))
            });
        }

        // Add total row
        result.Add(new SummaryDto
        {
            Category = "एकूण",
            Nivasi = result.Sum(r => r.Nivasi),
            Anivasi = result.Sum(r => r.Anivasi),
            Samyukt = result.Sum(r => r.Samyukt),
            Dharsthal = result.Sum(r => r.Dharsthal)
        });

        return result;
    }


}
