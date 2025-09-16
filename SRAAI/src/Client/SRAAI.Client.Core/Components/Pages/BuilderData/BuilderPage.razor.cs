using SRAAI.Shared.Dtos.AbhayYojana;

namespace SRAAI.Client.Core.Components.Pages.BuilderData;

public partial class BuilderPage : AppPageBase
{
    private BitFileUpload fileUploadRef = default!;
    private bool isBusy = false;
    
    private List<AbhayYojanaApplicationDto>? statistics;
    private bool isLoadingStatistics;

    [AutoInject] private HttpClient HttpClient = default!;
    [AutoInject] private DataService DataService = default!;

    protected override async Task OnInitAsync()
    {
        statistics = DataService.applications;
        await base.OnInitAsync();
        await LoadApplications();
    }
   
    private async Task LoadApplications()
    {
        try
        {
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

    private async void Cancel()
    {
        try
        {
            NavigationManager.NavigateTo($"{PageUrls.AbhayYojana}");
        }
        catch
        {

        }

    }


}

