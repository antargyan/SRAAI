using Microsoft.AspNetCore.Components.Routing;

namespace SRAAI.Client.Core.Components.Layout.Header;

public partial class SignOutConfirmDialog
{
    private bool isSigningOut;


    [Parameter] public bool IsOpen { get; set; }

    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }


    private async Task CloseModal()
    {
        if (isSigningOut) return;

        IsOpen = false;
        await IsOpenChanged.InvokeAsync(false);
    }

    private async Task SignOut()
    {
        if (isSigningOut) return;

        try
        {
            isSigningOut = true;

            await AuthManager.SignOut(CurrentCancellationToken);
            
            // Redirect to sign-in page after sign-out
            NavigationManager.NavigateTo(PageUrls.SignIn, forceLoad: true);
        }
        finally
        {
            isSigningOut = false;
        }

        await CloseModal();
    }

    private async Task HandleNavigation(LocationChangingContext context)
    {
        context.PreventNavigation();

        if (isSigningOut) return;

        IsOpen = false; 
        await IsOpenChanged.InvokeAsync(false);
    }
}
