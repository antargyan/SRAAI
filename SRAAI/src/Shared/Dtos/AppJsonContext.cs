using SRAAI.Shared.Dtos.Dashboard;
using SRAAI.Shared.Dtos.Products;
using SRAAI.Shared.Dtos.Categories;
using SRAAI.Shared.Dtos.PushNotification;
using SRAAI.Shared.Dtos.Identity;
using SRAAI.Shared.Dtos.Statistics;
using SRAAI.Shared.Dtos.Diagnostic;
using SRAAI.Shared.Dtos.AbhayYojana;
using SRAAI.Shared.Dtos.Summary;

namespace SRAAI.Shared.Dtos;

/// <summary>
/// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(Dictionary<string, string?>))]
[JsonSerializable(typeof(TimeSpan))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Guid[]))]
[JsonSerializable(typeof(GitHubStats))]
[JsonSerializable(typeof(NugetStatsDto))]
[JsonSerializable(typeof(AppProblemDetails))]
[JsonSerializable(typeof(SendNotificationToRoleDto))]
[JsonSerializable(typeof(PushNotificationSubscriptionDto))]
[JsonSerializable(typeof(CategoryDto))]
[JsonSerializable(typeof(List<CategoryDto>))]
[JsonSerializable(typeof(PagedResult<CategoryDto>))]
[JsonSerializable(typeof(ProductDto))]
[JsonSerializable(typeof(List<ProductDto>))]
[JsonSerializable(typeof(PagedResult<ProductDto>))]
[JsonSerializable(typeof(List<ProductsCountPerCategoryResponseDto>))]
[JsonSerializable(typeof(OverallAnalyticsStatsDataResponseDto))]
[JsonSerializable(typeof(List<ProductPercentagePerCategoryResponseDto>))]
[JsonSerializable(typeof(VerifyWebAuthnAndSignInRequestDto))]
[JsonSerializable(typeof(WebAuthnAssertionOptionsRequestDto))]
[JsonSerializable(typeof(AbhayYojanaApplicationDto))]
[JsonSerializable(typeof(CreateAbhayYojanaApplicationDto))]
[JsonSerializable(typeof(UpdateAbhayYojanaApplicationDto))]

[JsonSerializable(typeof(SummaryDto))]
[JsonSerializable(typeof(List<SummaryDto>))]
[JsonSerializable(typeof(PagedResult<SummaryDto>))]

[JsonSerializable(typeof(AbhayYojanaApplicationDto))]
[JsonSerializable(typeof(List<AbhayYojanaApplicationDto>))]
[JsonSerializable(typeof(PagedResult<AbhayYojanaApplicationDto>))]

[JsonSerializable(typeof(AbhayYojanaPagedResult))]
[JsonSerializable(typeof(List<AbhayYojanaPagedResult>))]
[JsonSerializable(typeof(PagedResult<AbhayYojanaPagedResult>))]

[JsonSerializable((typeof(UserDto)))]
[JsonSerializable((typeof(PagedResult<UserDto>)))]
[JsonSerializable((typeof(List<UserDto>)))]
public partial class AppJsonContext : JsonSerializerContext
{
}
