using SRAAI.Shared.Dtos.AbhayYojana;

namespace SRAAI.Client.Core.Components.Pages.BuilderData;

public partial class BuilderPage : AppPageBase
{
    private BitFileUpload fileUploadRef2 = default!;
    private bool isBusy = false;
    
    private List<AbhayYojanaApplicationDto>? statistics;
    private bool isLoadingStatistics;

    [AutoInject] private HttpClient HttpClient = default!;
    [AutoInject] private DataService DataService = default!;

    protected override async Task OnInitAsync()
    {
        
        await base.OnInitAsync();
        await LoadApplications();
    }
   
    private async Task LoadApplications()
    {
        try
        {
            statistics = DataService.applications;
            StateHasChanged();
        }
        catch (Exception)
        {
        }
        finally
        {
            StateHasChanged();
        }
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
    private async Task HandleBuilderUploadComplete(BitFileInfo info)
    {
        isBusy = false;
        try
        {
            DataService.applications = JsonSerializer.Deserialize<List<AbhayYojanaApplicationDto>>(info.Message!, JsonSerializerOptions);
            await LoadApplications();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            SnackBarService.Error($"Failed to process import result: {ex.Message}");
        }
    }

    private Task HandleUploadFailed(BitFileInfo info)
    {
        isBusy = false;
        SnackBarService.Error(string.IsNullOrWhiteSpace(info.Message) ? "File upload failed" : info.Message);
        return Task.CompletedTask;
    }
    private async void Cancel()
    {
        try
        {
            NavigationManager.NavigateTo($"{PageUrls.BuilderPage}");
        }
        catch
        {

        }

    }


}

