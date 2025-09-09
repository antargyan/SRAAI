using SRAAI.Server.Api.Models.Identity;

namespace SRAAI.Server.Api.Data.Configurations.Identity;

public partial class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
{
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        builder.HasIndex(userClaim => new { userClaim.UserId, userClaim.ClaimType, userClaim.ClaimValue });
    }
}
