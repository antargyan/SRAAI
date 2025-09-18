using ClosedXML.Excel;
using SRAAI.Shared.Dtos.AbhayYojana;
using SRAAI.Shared.Dtos.Summary;

namespace SRAAI.Client.Core.Components.Pages.AbhayYojana;

public partial class AbhayYojanaPage : AppPageBase
{
    private BitFileUpload fileUploadRef = default!;

    private bool isBusy = false;
    private bool isLoadingApplications = false;
    private bool isLoadingStatistics = false;
    
    private List<AbhayYojanaApplicationDto>? applications;
    private AbhayYojanaImportResult? lastImportResult;
    private AbhayYojanaStatistics? statistics;

    private List<SummaryDto>? summarydto;
    [Inject] private IJSRuntime JS { get; set; }
    [AutoInject] private HttpClient HttpClient = default!;
    [AutoInject] private DataService DataService = default!;
    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();
        await LoadApplications();
        await LoadStatistics();
    }

    private async Task LoadApplications()
    {
        try
        {
            isLoadingApplications = true;
            StateHasChanged();

            var url = new Uri(AbsoluteServerAddress, "/api/AbhayYojana/GetAll?page=1&pageSize=1000").ToString();
            var result = await HttpClient.GetFromJsonAsync<AbhayYojanaPagedResult>(url, JsonSerializerOptions, CurrentCancellationToken);
            applications = result?.Data ?? new List<AbhayYojanaApplicationDto>();
        }
        catch (Exception)
        {
            // Handle error
            applications = new List<AbhayYojanaApplicationDto>();
        }
        finally
        {
            isLoadingApplications = false;
            StateHasChanged();
        }
    }


    private async Task ExportToExcel()
    {
        try
        {
            var url = new Uri(AbsoluteServerAddress, "/api/AbhayYojana/Summary").ToString();
            summarydto = await HttpClient.GetFromJsonAsync<List<SummaryDto>>(
                url, JsonSerializerOptions, CurrentCancellationToken
            ) ?? new List<SummaryDto>();
        }
        catch (Exception)
        {
            summarydto = new List<SummaryDto>();
        }
        finally
        {
            isLoadingStatistics = false;
            StateHasChanged();
        }

        using var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Applications");

        worksheet.Cell(1, 1).Value = "Serial No";
        worksheet.Cell(1, 2).Value = "Slum Number";
        worksheet.Cell(1, 3).Value = "Original Dweller";
        worksheet.Cell(1, 4).Value = "Applicant Name";
        worksheet.Cell(1, 5).Value = "Voter Year";
        worksheet.Cell(1, 6).Value = "Voter Part";
        worksheet.Cell(1, 7).Value = "Voter Serial";
        worksheet.Cell(1, 8).Value = "Voter Bound";
        worksheet.Cell(1, 9).Value = "Slum Usage";
        worksheet.Cell(1, 10).Value = "Carpet Area";
        worksheet.Cell(1, 11).Value = "Evidence Details";
        worksheet.Cell(1, 12).Value = "Eligibility";
        worksheet.Cell(1, 13).Value = "Remarks";

        var headerRange = worksheet.Range(1, 1, 1, 13);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        for (int i = 0; i < applications.Count; i++)
        {
            var a = applications[i];
            int row = i + 2;
            worksheet.Cell(row, 1).Value = a.SerialNumber;
            worksheet.Cell(row, 2).Value = a.OriginalSlumNumber;
            worksheet.Cell(row, 3).Value = a.OriginalSlumDwellerName;
            worksheet.Cell(row, 4).Value = a.ApplicantName;
            worksheet.Cell(row, 5).Value = a.VoterListYear;
            worksheet.Cell(row, 6).Value = a.VoterListPartNumber;
            worksheet.Cell(row, 7).Value = a.VoterListSerialNumber;
            worksheet.Cell(row, 8).Value = a.VoterListBound;
            worksheet.Cell(row, 9).Value = a.SlumUsage;
            worksheet.Cell(row, 10).Value = a.CarpetAreaSqFt;
            worksheet.Cell(row, 11).Value = a.EvidenceDetails;
            worksheet.Cell(row, 12).Value = a.EligibilityStatus;
            worksheet.Cell(row, 13).Value = a.Remarks;
        }

        worksheet.Columns().AdjustToContents();

        var summarySheet = workbook.Worksheets.Add("Summary");

        string[] headers = { "वापर", "निवासी", "अनिवासी", "संयुक्त", "धारस्थळ", "एकूण" };

        for (int i = 0; i < headers.Length; i++)
        {
            summarySheet.Cell(1, i + 1).Value = headers[i];
        }

        var summaryHeaderRange = summarySheet.Range(1, 1, 1, headers.Length);
        summaryHeaderRange.Style.Font.Bold = true;
        summaryHeaderRange.Style.Fill.BackgroundColor = XLColor.LightPink;
        summaryHeaderRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        summaryHeaderRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        int summaryRow = 2;
        foreach (var item in summarydto)
        {
            summarySheet.Cell(summaryRow, 1).Value = item.Category;
            summarySheet.Cell(summaryRow, 2).Value = item.Nivasi;
            summarySheet.Cell(summaryRow, 3).Value = item.Anivasi;
            summarySheet.Cell(summaryRow, 4).Value = item.Samyukt;
            summarySheet.Cell(summaryRow, 5).Value = item.Dharsthal;
            summarySheet.Cell(summaryRow, 6).Value = item.Total;
            summaryRow++;
        }

        summarySheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();

        await JS.InvokeVoidAsync("downloadFile",
            "Applications.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Convert.ToBase64String(bytes));
    }


    private async void GoToSummary()
    {
        try
        {
            NavigationManager.NavigateTo($"{PageUrls.SummaryPage}");
        }
        catch
        {}
    }
    private async Task LoadStatistics()
    {
        try
        {
            isLoadingStatistics = true;
            StateHasChanged();

            var url = new Uri(AbsoluteServerAddress, "/api/AbhayYojana/GetStatistics").ToString();
            statistics = await HttpClient.GetFromJsonAsync<AbhayYojanaStatistics>(url, JsonSerializerOptions, CurrentCancellationToken);
        }
        catch (Exception)
        {}
        finally
        {
            isLoadingStatistics = false;
            StateHasChanged();
        }
    }

    private async Task<string> GetUploadUrl()
    {
        return new Uri(AbsoluteServerAddress, "/api/AbhayYojana/ImportExcel").ToString();
    }

    private async Task<Dictionary<string, string>> GetUploadRequestHeaders()
    {
        var token = await AuthManager.GetFreshAccessToken(requestedBy: nameof(BitFileUpload));
        return new() { { "Authorization", $"Bearer {token}" } };
    }
    private Task HandleUploadFailed(BitFileInfo info)
    {
        isBusy = false;
        SnackBarService.Error(string.IsNullOrWhiteSpace(info.Message) ? "File upload failed" : info.Message);
        return Task.CompletedTask;
    }

    private async Task HandleUploadComplete(BitFileInfo info)
    {
        isBusy = false;
        try
        { 
            await LoadApplications();
            await LoadStatistics();
            
            // For now, just show success message
            // The import result will be displayed when the data is refreshed
            SnackBarService.Success("Excel imported successfully. Please check the results below.");
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            SnackBarService.Error($"Failed to process import result: {ex.Message}");
        }
    }

  
}

public record AbhayYojanaImportResult(
    int TotalRows,
    int SuccessfullyImported,
    int SkippedRows,
    List<string> Errors,
    List<string> Warnings
);

/*public record AbhayYojanaPagedResult(
    List<AbhayYojanaApplicationDto> Data,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);*/

public record AbhayYojanaStatistics(
    int TotalApplications,
    List<AbhayYojanaStatusBreakdown> StatusBreakdown,
    DateTime? LastUpdated
);

public record AbhayYojanaStatusBreakdown(
    string Status,
    int Count
);
