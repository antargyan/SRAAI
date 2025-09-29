using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using SRAAI.Shared.Dtos.AbhayYojana;
using SRAAI.Shared.Dtos.Summary;

namespace SRAAI.Client.Core.Components.Pages.AbhayYojana;

public partial class AbhayYojanaPage : AppPageBase
{
    private BitFileUpload fileUploadRef = default!;
    private BitFileUpload fileUpdateRef = default!;

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
        var bytes = await HttpClient.GetByteArrayAsync("/api/AbhayYojana/ExportExcel");
        await JS.InvokeVoidAsync("downloadFile",
            "Applications.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Convert.ToBase64String(bytes));
    }

    private async Task DownloadSampleExcel()
    {
        try
        {
            var bytes = await HttpClient.GetByteArrayAsync("/api/AbhayYojana/DownloadSampleExcel");
            await JS.InvokeVoidAsync("downloadFile",
                "AbhayYojana_Sample_Template.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Convert.ToBase64String(bytes));
            
            SnackBarService.Success("Sample Excel template downloaded successfully!");
        }
        catch (Exception ex)
        {
            SnackBarService.Error($"Failed to download sample template: {ex.Message}");
        }
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

    private async Task<string> GetUpdateUploadUrl()
    {
        return new Uri(AbsoluteServerAddress, "/api/AbhayYojana/ImportUpdateExcel").ToString();
    }

    private async Task<Dictionary<string, string>> GetUpdateUploadRequestHeaders()
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
    private async Task HandleUploadUpdateComplete(BitFileInfo info)
    {
        isBusy = false;
        try
        {
            await LoadApplications();
            await LoadStatistics();

            // For now, just show success message
            // The import result will be displayed when the data is refreshed
            SnackBarService.Success("Addendum Excel Update successfully. Please check the results below.");

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
