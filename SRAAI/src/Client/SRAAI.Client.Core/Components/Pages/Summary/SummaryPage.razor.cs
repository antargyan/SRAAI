using SRAAI.Shared.Dtos.Summary;

namespace SRAAI.Client.Core.Components.Pages.Summary;

public partial class SummaryPage : AppPageBase
{
    private BitFileUpload fileUploadRef = default!;
    private bool isBusy = false;
    
    private List<SummaryDto>? statistics;
    private bool isLoadingStatistics;

    [AutoInject] private HttpClient HttpClient = default!;

    protected override async Task OnInitAsync()
    {
        await LoadStatistics();
        await base.OnInitAsync();
        await LoadApplications();
    }
    private async Task LoadStatistics()
    {
        try
        {
            isLoadingStatistics = true;
            StateHasChanged();

            var url = new Uri(AbsoluteServerAddress, "/api/AbhayYojana/Summary").ToString();
            statistics = await HttpClient.GetFromJsonAsync<List<SummaryDto>>(
                url, JsonSerializerOptions, CurrentCancellationToken
            ) ?? new List<SummaryDto>();
        }
        catch (Exception ex)
        {
            statistics = new List<SummaryDto>();
        }
        finally
        {
            isLoadingStatistics = false;
            StateHasChanged();
        }
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
    private async Task<string> GoToSummary()
    {
        return new Uri(AbsoluteServerAddress, "/api/AbhayYojana/Summary").ToString();
    }
  
}

