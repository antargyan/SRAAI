namespace SRAAI.Client.Core.Components.Pages.Excel;

public partial class ExcelImportPage
{
	private BitFileUpload fileUploadRef = default!;
	private bool isBusy;
	private string dataset = string.Empty;
	[AutoInject] private HttpClient HttpClient = default!;
	private ImportResultVm? lastResult;
	private List<ImportSessionVm>? history;
	private List<ChangeVm>? changes;
	private int selectedVersion;
	private bool isLoadingHistory;

	private async Task<string> GetUploadUrl()
	{
		var baseUrl = new Uri(AbsoluteServerAddress, $"/api/Excel/Import?dataset={Uri.EscapeDataString(dataset)}").ToString();
		if (CultureInfoManager.InvariantGlobalization is false)
		{
			baseUrl += $"&culture={CultureInfo.CurrentUICulture.Name}";
		}
		return baseUrl;
	}

	private async Task<Dictionary<string, string>> GetUploadRequestHeaders()
	{
		var token = await AuthManager.GetFreshAccessToken(requestedBy: nameof(BitFileUpload));
		return new() { { "Authorization", $"Bearer {token}" } };
	}

	private async Task HandleUploadComplete(BitFileInfo _)
	{
		isBusy = false;
		try
		{
			await LoadHistory();
			SnackBarService.Success("Excel imported successfully.");
		}
		catch (KnownException e)
		{
			SnackBarService.Error(e.Message);
		}
	}

	private Task HandleUploadFailed(BitFileInfo info)
	{
		isBusy = false;
		SnackBarService.Error(string.IsNullOrWhiteSpace(info.Message) ? Localizer[nameof(AppStrings.FileUploadFailed)] : info.Message);
		return Task.CompletedTask;
	}

	protected override async Task OnInitAsync()
	{
		await base.OnInitAsync();
		await LoadHistory();
	}

	private async Task LoadHistory()
	{
		if (string.IsNullOrWhiteSpace(dataset)) { history = null; changes = null; StateHasChanged(); return; }
		isLoadingHistory = true;
		try
		{
			var url = new Uri(AbsoluteServerAddress, $"/api/Excel/History?dataset={Uri.EscapeDataString(dataset)}").ToString();
			history = await HttpClient.GetFromJsonAsync<List<ImportSessionVm>>(url, JsonSerializerOptions, CurrentCancellationToken);
		}
		finally
		{
			isLoadingHistory = false;
		}
	}

	private async Task LoadChanges(int version)
	{
		selectedVersion = version;
		var url = new Uri(AbsoluteServerAddress, $"/api/Excel/Changes?dataset={Uri.EscapeDataString(dataset)}&version={version}").ToString();
		changes = await HttpClient.GetFromJsonAsync<List<ChangeVm>>(url, JsonSerializerOptions, CurrentCancellationToken);
	}

	private sealed record ImportResultVm(int NewVersionNo, int Inserted, int Updated, int Deleted, string? AiSummaryEn, string? AiSummaryMr, Guid SessionId);
	private sealed record ImportSessionVm(Guid Id, string DatasetName, int NewVersionNo, int PreviousVersionNo, int InsertedCount, int UpdatedCount, int DeletedCount, string? AiSummaryEn, string? AiSummaryMr, DateTimeOffset CreatedAt);
	private sealed record ChangeVm(Guid Id, string DatasetName, string BusinessKey, int VersionNo, string ChangeType, string? DataJson, DateTimeOffset CreatedAt);
}


