using SRAAI.Shared.Controllers.SystemUsers;
using SRAAI.Shared.Dtos.Identity;

namespace SRAAI.Client.Core.Components.Pages.SystemUsers;

[Authorize]
public partial class UsersPage
{

    [AutoInject] ISystemUserController userController = default!;

    private bool isLoading;
    private bool isDeleteDialogOpen;
    private UserDto? deletingUser;
    private BitDataGrid<UserDto>? dataGrid;
    private string userFullNameFilter = string.Empty;

    private BitDataGridItemsProvider<UserDto> usersProvider = default!;
    private BitDataGridPaginationState pagination = new() { ItemsPerPage = 10 };

    private string UserFullNameFilter
    {
        get => userFullNameFilter;
        set
        {
            userFullNameFilter = value;
            _ = RefreshData();
        }
    }


    protected override async Task OnInitAsync()
    {
        PrepareGridDataProvider();

        await base.OnInitAsync();
    }

    private void PrepareGridDataProvider()
    {
        usersProvider = async req =>
        {
            isLoading = true;

            try
            {
                var odataQ = new ODataQuery
                {
                    Top = req.Count ?? 10,
                    Skip = req.StartIndex,
                    OrderBy = string.Join(", ", req.GetSortByProperties().Select(p => $"{p.PropertyName} {(p.Direction == BitDataGridSortDirection.Ascending ? "asc" : "desc")}"))
                };

                if (string.IsNullOrEmpty(UserFullNameFilter) is false)
                {
                    odataQ.Filter = $"contains(tolower({nameof(UserDto.FullName)}),'{UserFullNameFilter.ToLower()}')";
                }

                var data = await userController.GetUsers(CurrentCancellationToken);

                return BitDataGridItemsProviderResult.From(data!.Items!, (int)data!.TotalCount);
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
                return BitDataGridItemsProviderResult.From(new List<UserDto> { }, 0);
            }
            finally
            {
                isLoading = false;

                StateHasChanged();
            }
        };
    }

    private async Task RefreshData()
    {
        await dataGrid!.RefreshDataAsync();
    }

    private async Task CreateUser()
    {
        NavigationManager.NavigateTo(PageUrls.AddOrEditUserPage);
    }

    private async Task EditUser(UserDto user)
    {
        NavigationManager.NavigateTo($"{PageUrls.AddOrEditUserPage}/{user.FullName}");
    }

    private async Task DeleteUser()
    {
        if (deletingUser is null) return;

        try
        {
            await userController.Delete(
                deletingUser.Id,  
                CurrentCancellationToken
            );

            await RefreshData();
        }
        catch (KnownException exp)
        {
            SnackBarService.Error(exp.Message);
        }
        finally
        {
            deletingUser = null;
        }
    }
}
