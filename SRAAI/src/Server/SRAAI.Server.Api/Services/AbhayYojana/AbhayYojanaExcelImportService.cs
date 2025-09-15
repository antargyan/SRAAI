using Microsoft.EntityFrameworkCore;
using SRAAI.Server.Api.Data;
using SRAAI.Server.Api.Models.Excel;
using SRAAI.Shared.Dtos.AbhayYojana;
using System.Text.Json;

namespace SRAAI.Server.Api.Services.AbhayYojana;

public interface IAbhayYojanaExcelImportService
{
    Task<AbhayYojanaImportResult> ImportFromExcelAsync(Stream excelStream, CancellationToken cancellationToken = default);
}

public record AbhayYojanaImportResult(
    int TotalRows,
    int SuccessfullyImported,
    int SkippedRows,
    List<string> Errors,
    List<string> Warnings
);

public partial class AbhayYojanaExcelImportService : IAbhayYojanaExcelImportService
{
    [AutoInject] private AppDbContext dbContext = default!;

    public async Task<AbhayYojanaImportResult> ImportFromExcelAsync(Stream excelStream, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var successfullyImported = 0;
        var skippedRows = 0;
        var totalRows = 0;

        try
        {
            // Create a memory stream to avoid stream disposal issues
            using var memoryStream = new MemoryStream();
            await excelStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            using var workbook = new ClosedXML.Excel.XLWorkbook(memoryStream);
            var worksheet = workbook.Worksheets.FirstOrDefault() 
                ?? throw new InvalidOperationException("Excel file has no worksheets");

            // Find header row
            var firstRow = worksheet.FirstRowUsed() ?? worksheet.Row(1);
            var headers = firstRow.Cells().Select(c => c.GetString().Trim()).ToArray();

            if (headers.Length == 0)
                throw new InvalidOperationException("Excel file has no header row");

            // Map headers to our expected columns
            var headerMapping = MapHeaders(headers);
            ValidateRequiredHeaders(headerMapping, errors);

            if (errors.Any())
                return new AbhayYojanaImportResult(0, 0, 0, errors, warnings);

            // Process data rows
            int startRow = firstRow.RowNumber() + 1;
            var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? (startRow - 1);

            var applicationsToImport = new List<AbhayYojanaApplication>();

            for (int rowNum = startRow; rowNum <= lastRowNumber; rowNum++)
            {
                totalRows++;
                var row = worksheet.Row(rowNum);
                
                if (row.IsEmpty())
                {
                    skippedRows++;
                    continue;
                }

                try
                {
                    var application = ParseRowToApplication(row, headerMapping, rowNum);
                    if (application != null)
                    {
                        applicationsToImport.Add(application);
                        successfullyImported++;
                    }
                    else
                    {
                        skippedRows++;
                        // Add warning for skipped records due to empty Column L
                        warnings.Add($"Row {rowNum}: Skipped due to empty data in Column L (Eligibility Status)");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {rowNum}: {ex.Message}");
                    skippedRows++;
                }
            }

            // Validate for duplicate primary keys
            var duplicateKeys = applicationsToImport
                .GroupBy(a => a.OriginalSlumNumber)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key.ToString())
                .ToList();

            if (duplicateKeys.Any())
            {
                errors.Add($"Duplicate primary keys found: {string.Join(", ", duplicateKeys)}");
                return new AbhayYojanaImportResult(totalRows, 0, skippedRows, errors, warnings);
            }

            // Save to database
            if (applicationsToImport.Any())
            {
                using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Get the next version number for this import batch
                    var nextVersion = await GetNextVersionNumberAsync(cancellationToken);

                    // Check for existing records and update or insert
                    var existingSlumNumbers = applicationsToImport.Select(a => a.OriginalSlumNumber).ToList();
                    var existingApplications = await dbContext.AbhayYojanaApplications
                        .Where(a => existingSlumNumbers.Contains(a.OriginalSlumNumber))
                        .ToListAsync(cancellationToken);

                    var existingSlumNumbersSet = existingApplications.Select(a => a.OriginalSlumNumber).ToHashSet();

                    foreach (var application in applicationsToImport)
                    {
                        if (existingSlumNumbersSet.Contains(application.OriginalSlumNumber))
                        {
                            // Update existing - increment version
                            var existing = existingApplications.First(a => a.OriginalSlumNumber == application.OriginalSlumNumber);
                            UpdateApplication(existing, application);
                            existing.Version = nextVersion; // Increment version for updates
                            existing.UpdatedDate = DateTime.UtcNow;
                        }
                        else
                        {
                            // Insert new - set version to current batch version
                            application.CreatedDate = DateTime.UtcNow;
                            application.UpdatedDate = null; // Ensure UpdatedDate is null for new records
                            application.Version = nextVersion; // Set version for new records
                            await dbContext.AbhayYojanaApplications.AddAsync(application, cancellationToken);
                        }
                    }

                    var savedCount = await dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    
                    // Log successful save
                    if (savedCount > 0)
                    {
                        warnings.Add($"Successfully saved {savedCount} records to database with version {nextVersion}");
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    errors.Add($"Database save failed: {ex.Message}");
                    errors.Add($"Stack trace: {ex.StackTrace}");
                    return new AbhayYojanaImportResult(totalRows, 0, skippedRows, errors, warnings);
                }
            }

            return new AbhayYojanaImportResult(totalRows, successfullyImported, skippedRows, errors, warnings);
        }
        catch (Exception ex)
        {
            errors.Add($"Import failed: {ex.Message}");
            errors.Add($"Stack trace: {ex.StackTrace}");
            return new AbhayYojanaImportResult(totalRows, 0, skippedRows, errors, warnings);
        }
    }

    private Dictionary<string, int> MapHeaders(string[] headers)
    {
        var mapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        for (int i = 0; i < headers.Length; i++)
        {
            var header = headers[i].Trim();
            if (string.IsNullOrEmpty(header)) continue;

            // Map various possible header names to our standard names
            if (IsHeaderMatch(header, ["अ.क्र.", "Serial Number", "Sr. No.", "Serial No"]))
                mapping["SerialNumber"] = i;
            else if (IsHeaderMatch(header, ["मूळ परिशिष्ट-॥ मधील झो.क्र.", "Original Slum Number", "Slum No", "झो.क्र."]))
                mapping["OriginalSlumNumber"] = i;
            else if (IsHeaderMatch(header, ["मूळ परिशिष्ट-॥ मधील झोपडीधारकाचे नाव", "Original Slum Dweller Name", "Original Dweller"]))
                mapping["OriginalSlumDwellerName"] = i;
            else if (IsHeaderMatch(header, ["विहीत जोडपत्र दाखल केलेल्या अर्जदाराचे नाव", "Applicant Name", "अर्जदाराचे नाव"]))
                mapping["ApplicantName"] = i;
            else if (IsHeaderMatch(header, ["वर्ष", "Year", "Voter Year"]))
                mapping["VoterListYear"] = i;
            else if (IsHeaderMatch(header, ["भाग क्र.", "Part No", "Part Number"]))
                mapping["VoterListPartNumber"] = i;
            else if (IsHeaderMatch(header, ["मतदार यादीतील अ.क्र.", "Voter List Sr. No", "Voter Serial"]))
                mapping["VoterListSerialNumber"] = i;
            else if (IsHeaderMatch(header, ["मतदार यादीतील बांध", "Voter List Bound", "Voter Bound"]))
                mapping["VoterListBound"] = i;
            else if (IsHeaderMatch(header, ["झोपडीचा वापर", "Slum Usage", "Usage"]))
                mapping["SlumUsage"] = i;
            else if (IsHeaderMatch(header, ["झोपडी खालील चटई क्षेत्र", "Carpet Area", "Area Sq Ft", "चौ.फू."]))
                mapping["CarpetAreaSqFt"] = i;
            else if (IsHeaderMatch(header, ["अर्जदार यांनी अभय योजनेनुसार सादर केलेल्या प्रमुख पुराव्यांचा तपशील", "Evidence Details", "Evidence", "पुराव्यांचा तपशील"]))
                mapping["EvidenceDetails"] = i;
            else if (IsHeaderMatch(header, ["पात्र/अपात्र/अनिर्णित", "Eligibility Status", "Status", "पात्रता"]))
                mapping["EligibilityStatus"] = i;
            else if (IsHeaderMatch(header, ["शेरा", "Remarks", "Comments"]))
                mapping["Remarks"] = i;
        }

        return mapping;
    }

    private bool IsHeaderMatch(string header, string[] possibleMatches)
    {
        return possibleMatches.Any(match => 
            header.Contains(match, StringComparison.OrdinalIgnoreCase) ||
            match.Contains(header, StringComparison.OrdinalIgnoreCase));
    }

    private void ValidateRequiredHeaders(Dictionary<string, int> mapping, List<string> errors)
    {
        var requiredHeaders = new[] { "OriginalSlumNumber", "OriginalSlumDwellerName", "ApplicantName", "SlumUsage", "EvidenceDetails", "EligibilityStatus" };
        
        foreach (var required in requiredHeaders)
        {
            if (!mapping.ContainsKey(required))
            {
                errors.Add($"Required column '{required}' not found in Excel file");
            }
        }
    }

    private AbhayYojanaApplication? ParseRowToApplication(ClosedXML.Excel.IXLRow row, Dictionary<string, int> mapping, int rowNumber)
    {
        try
        {
            // Check Column L (12th column) - if empty, skip this record
            // First try to find by header mapping, then fall back to column position
            bool shouldSkip = false;
            
            if (mapping.TryGetValue("EligibilityStatus", out int eligibilityIndex))
            {
                var eligibilityValue = row.Cell(eligibilityIndex + 1).GetString().Trim();
                if (string.IsNullOrEmpty(eligibilityValue))
                {
                    shouldSkip = true;
                }
            }
            else
            {
                // Fallback: Check Column L (12th column) directly
                var columnLValue = row.Cell(12).GetString().Trim();
                if (string.IsNullOrEmpty(columnLValue))
                {
                    shouldSkip = true;
                }
            }
            
            if (shouldSkip)
            {
                // Skip this record if Column L is empty
                return null;
            }

            var application = new AbhayYojanaApplication();

            // Parse Serial Number (Optional)
            if (mapping.TryGetValue("SerialNumber", out int serialIndex))
            {
                var serialValue = row.Cell(serialIndex + 1).GetString().Trim();
                if (int.TryParse(serialValue, out int serial))
                    application.SerialNumber = serial;
                else
                    application.SerialNumber = null;
            }

            // Parse Original Slum Number (Primary Key - Required)
            if (!mapping.TryGetValue("OriginalSlumNumber", out int slumIndex))
                throw new InvalidOperationException("Original Slum Number column not found");
            
            var slumValue = row.Cell(slumIndex + 1).GetString().Trim();
            if (string.IsNullOrEmpty(slumValue) || !int.TryParse(slumValue, out int slumNumber))
                throw new InvalidOperationException("Original Slum Number is required and must be a valid number");
            
            application.OriginalSlumNumber = slumNumber;

            // Parse Original Slum Dweller Name (Required)
            if (!mapping.TryGetValue("OriginalSlumDwellerName", out int dwellerIndex))
                throw new InvalidOperationException("Original Slum Dweller Name column not found");
            
            application.OriginalSlumDwellerName = row.Cell(dwellerIndex + 1).GetString().Trim();
            if (string.IsNullOrEmpty(application.OriginalSlumDwellerName))
                throw new InvalidOperationException("Original Slum Dweller Name is required");

            // Parse Applicant Name (Required)
            if (!mapping.TryGetValue("ApplicantName", out int applicantIndex))
                throw new InvalidOperationException("Applicant Name column not found");
            
            application.ApplicantName = row.Cell(applicantIndex + 1).GetString().Trim();
            if (string.IsNullOrEmpty(application.ApplicantName))
                throw new InvalidOperationException("Applicant Name is required");

            // Parse Voter List details (Optional)
            if (mapping.TryGetValue("VoterListYear", out int yearIndex))
            {
                var yearValue = row.Cell(yearIndex + 1).GetString().Trim();
                if (int.TryParse(yearValue, out int year))
                    application.VoterListYear = year;
            }

            if (mapping.TryGetValue("VoterListPartNumber", out int partIndex))
            {
                application.VoterListPartNumber = row.Cell(partIndex + 1).GetString().Trim();
                if (string.IsNullOrEmpty(application.VoterListPartNumber))
                    application.VoterListPartNumber = null;
            }

            if (mapping.TryGetValue("VoterListSerialNumber", out int voterSerialIndex))
            {
                var voterSerialValue = row.Cell(voterSerialIndex + 1).GetString().Trim();
                if (int.TryParse(voterSerialValue, out int voterSerial))
                    application.VoterListSerialNumber = voterSerial;
            }

            if (mapping.TryGetValue("VoterListBound", out int boundIndex))
            {
                application.VoterListBound = row.Cell(boundIndex + 1).GetString().Trim();
                if (string.IsNullOrEmpty(application.VoterListBound))
                    application.VoterListBound = null;
            }

            // Parse Slum Usage (Required)
            if (!mapping.TryGetValue("SlumUsage", out int usageIndex))
                throw new InvalidOperationException("Slum Usage column not found");
            
            application.SlumUsage = row.Cell(usageIndex + 1).GetString().Trim();
            if (string.IsNullOrEmpty(application.SlumUsage))
                throw new InvalidOperationException("Slum Usage is required");

            // Parse Carpet Area (Optional)
            if (mapping.TryGetValue("CarpetAreaSqFt", out int areaIndex))
            {
                var areaValue = row.Cell(areaIndex + 1).GetString().Trim();
                if (decimal.TryParse(areaValue, out decimal area))
                    application.CarpetAreaSqFt = area;
            }

            // Parse Evidence Details (Required)
            if (!mapping.TryGetValue("EvidenceDetails", out int evidenceIndex))
                throw new InvalidOperationException("Evidence Details column not found");
            
            application.EvidenceDetails = row.Cell(evidenceIndex + 1).GetString().Trim();
            if (string.IsNullOrEmpty(application.EvidenceDetails))
                throw new InvalidOperationException("Evidence Details is required");

            // Parse Eligibility Status (Required)
            if (!mapping.TryGetValue("EligibilityStatus", out int statusIndex))
                throw new InvalidOperationException("Eligibility Status column not found");
            
            application.EligibilityStatus = row.Cell(statusIndex + 1).GetString().Trim();
            if (string.IsNullOrEmpty(application.EligibilityStatus))
                throw new InvalidOperationException("Eligibility Status is required");

            // Parse Remarks (Optional)
            if (mapping.TryGetValue("Remarks", out int remarksIndex))
            {
                application.Remarks = row.Cell(remarksIndex + 1).GetString().Trim();
                if (string.IsNullOrEmpty(application.Remarks))
                    application.Remarks = null;
            }

            return application;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing row {rowNumber}: {ex.Message}", ex);
        }
    }

    private void UpdateApplication(AbhayYojanaApplication existing, AbhayYojanaApplication updated)
    {
        existing.SerialNumber = updated.SerialNumber;
        existing.OriginalSlumDwellerName = updated.OriginalSlumDwellerName;
        existing.ApplicantName = updated.ApplicantName;
        existing.VoterListYear = updated.VoterListYear;
        existing.VoterListPartNumber = updated.VoterListPartNumber;
        existing.VoterListSerialNumber = updated.VoterListSerialNumber;
        existing.VoterListBound = updated.VoterListBound;
        existing.SlumUsage = updated.SlumUsage;
        existing.CarpetAreaSqFt = updated.CarpetAreaSqFt;
        existing.EvidenceDetails = updated.EvidenceDetails;
        existing.EligibilityStatus = updated.EligibilityStatus;
        existing.Remarks = updated.Remarks;
        // Note: Version is handled separately in the calling method
    }

    private async Task<int> GetNextVersionNumberAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Try to get the maximum version number
            var maxVersion = await dbContext.AbhayYojanaApplications
                .MaxAsync(a => (int?)a.Version, cancellationToken) ?? 0;
            return maxVersion + 1;
        }
        catch (Exception)
        {
            // If Version column doesn't exist or there's any error, start with version 1
            return 1;
        }
    }
}
