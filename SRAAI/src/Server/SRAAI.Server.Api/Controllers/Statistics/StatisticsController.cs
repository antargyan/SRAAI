using SRAAI.Server.Api.Services;
using SRAAI.Shared.Dtos.Statistics;
using SRAAI.Shared.Controllers.Statistics;

namespace SRAAI.Server.Api.Controllers.Statistics;

[ApiController, Route("api/[controller]/[action]")]
public partial class StatisticsController : AppControllerBase, IStatisticsController
{
    [AutoInject] private NugetStatisticsService nugetHttpClient = default!;

    [AllowAnonymous]
    [HttpGet("{packageId}")]
    [AppResponseCache(MaxAge = 3600 * 24, UserAgnostic = true)]
    public async Task<NugetStatsDto> GetNugetStats(string packageId, CancellationToken cancellationToken)
    {
        return await nugetHttpClient.GetPackageStats(packageId, cancellationToken);
    }
}
