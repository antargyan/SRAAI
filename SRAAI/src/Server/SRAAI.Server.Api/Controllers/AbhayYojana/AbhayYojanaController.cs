using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SRAAI.Server.Api.Data;
using SRAAI.Server.Api.Services.AbhayYojana;
using SRAAI.Shared.Dtos.AbhayYojana;

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

            if (file.Length > 50 * 1024 * 1024) // 50MB limit
                throw new BadRequestException("File size exceeds 50MB limit");

            await using var stream = file.OpenReadStream();
            var result = await excelImportService.ImportFromExcelAsync(stream, cancellationToken);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            // You can add proper logging here
            return BadRequest(new { 
                error = "Import failed", 
                message = ex.Message,
                details = ex.StackTrace 
            });
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
}
