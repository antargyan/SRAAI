using SRAAI.Shared.Dtos.AbhayYojana;

namespace SRAAI.Client.Core.Components.Pages.AbhayYojana;

public partial class AbhayYojanaPage : AppPageBase
{
    private BitFileUpload fileUploadRef = default!;
    private BitFileUpload fileUploadRef2 = default!;
    private bool isBusy = false;
    private bool isLoadingApplications = false;
    private bool isLoadingStatistics = false;
    
    private List<AbhayYojanaApplicationDto>? applications;
    private AbhayYojanaImportResult? lastImportResult;
    private AbhayYojanaStatistics? statistics;
    
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
    private async void GoToSummary()
    {
        try
        {
            NavigationManager.NavigateTo($"{PageUrls.SummaryPage}");
        }
        catch
        {

        }

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
        {
            // Handle error
        }
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


    private async Task<string> GetBuilderUploadUrl()
    {
        return new Uri(AbsoluteServerAddress, "/api/AbhayYojana/BuilderExcelScanning").ToString();
    }

    private async Task<Dictionary<string, string>> GetBuilderUploadRequestHeaders()
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

    private async Task HandleBuilderUploadComplete(BitFileInfo info)
    {
        isBusy = false;
        try
        {
            DataService.applications = JsonSerializer.Deserialize<List<AbhayYojanaApplicationDto>>(info.Message!, JsonSerializerOptions);
            NavigationManager.NavigateTo($"{PageUrls.BuilderPage}");

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

public record AbhayYojanaPagedResult(
    List<AbhayYojanaApplicationDto> Data,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public record AbhayYojanaStatistics(
    int TotalApplications,
    List<AbhayYojanaStatusBreakdown> StatusBreakdown,
    DateTime? LastUpdated
);

public record AbhayYojanaStatusBreakdown(
    string Status,
    int Count
);
