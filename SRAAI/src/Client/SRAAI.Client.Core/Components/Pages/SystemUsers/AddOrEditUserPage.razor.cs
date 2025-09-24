using SRAAI.Shared.Controllers.SystemUsers;
using SRAAI.Shared.Dtos.Identity;

namespace SRAAI.Client.Core.Components.Pages.SystemUsers;

[Authorize]
public partial class AddOrEditUserPage
{

    [AutoInject] ISystemUserController userController = default!;

    [Parameter] public Guid? Id { get; set; }

    private bool isLoading;
    private bool isSaving;
    
    private UserDto user = new();

    protected override async Task OnInitAsync()
    {
        await LoadUser();
    }

    private async Task LoadUser()
    {
        if (Id is null) return;

        isLoading = true;

        try
        {
            user = await userController.Get(Id.Value, CurrentCancellationToken);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task Save()
    {
        if (isSaving) return;

        isSaving = true;

        try
        {
            if (Id == default)
            {
                await userController.Create(user, CurrentCancellationToken);
            }
            else
            {
                await userController.Update(user, CurrentCancellationToken);
            }

            NavigationManager.NavigateTo(PageUrls.UsersPage);
        }
        catch (ResourceValidationException e)
        {
            SnackBarService.Error(string.Join(Environment.NewLine, e.Payload.Details.SelectMany(d => d.Errors).Select(e => e.Message)));
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            isSaving = false;
        }
    }
}
