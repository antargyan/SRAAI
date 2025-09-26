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
    private string userFullNameFilter = string.Empty;
    private List<UserDto>? users;

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
        await LoadUsers();
        await base.OnInitAsync();
    }

    private async Task LoadUsers()
    {
        isLoading = true;
        StateHasChanged();

        try
        {
            var data = await userController.GetUsers(CurrentCancellationToken);
            users = data?.Items?.ToList() ?? new List<UserDto>();
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
            users = new List<UserDto>();
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task RefreshData()
    {
        if (string.IsNullOrEmpty(UserFullNameFilter))
        {
            await LoadUsers();
        }
        else
        {
            // Filter users locally for better performance
            var allUsers = await userController.GetUsers(CurrentCancellationToken);
            var filteredUsers = allUsers?.Items?.Where(u => 
                (u.FullName?.Contains(UserFullNameFilter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.UserName?.Contains(UserFullNameFilter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.Email?.Contains(UserFullNameFilter, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList() ?? new List<UserDto>();
            
            users = filteredUsers;
            StateHasChanged();
        }
    }

    private async Task CreateUser()
    {
        NavigationManager.NavigateTo(PageUrls.AddOrEditUserPage);
    }

    private async Task EditUser(UserDto user)
    {
        NavigationManager.NavigateTo($"{PageUrls.AddOrEditUserPage}/{user.Id}");
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
