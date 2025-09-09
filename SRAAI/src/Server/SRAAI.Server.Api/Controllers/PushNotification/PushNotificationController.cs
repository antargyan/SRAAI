using SRAAI.Server.Api.Services;
using SRAAI.Shared.Dtos.PushNotification;
using SRAAI.Shared.Controllers.PushNotification;

namespace SRAAI.Server.Api.Controllers.PushNotification;

[Route("api/[controller]/[action]")]
[ApiController, AllowAnonymous]
public partial class PushNotificationController : AppControllerBase, IPushNotificationController
{
    [AutoInject] PushNotificationService pushNotificationService = default!;

    [HttpPost]
    public async Task Subscribe([Required] PushNotificationSubscriptionDto subscription, CancellationToken cancellationToken)
    {
        HttpContext.ThrowIfContainsExpiredAccessToken();

        await pushNotificationService.Subscribe(subscription, cancellationToken);
    }
}
