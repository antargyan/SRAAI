namespace SRAAI.Client.Core.Components.Layout;

public partial class MainLayout
{
    private List<BitNavItem> navPanelItems = [];

    [AutoInject] protected IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] protected IAuthorizationService authorizationService = default!;

    private async Task SetNavPanelItems(ClaimsPrincipal authUser)
    {
        navPanelItems =
        [
            new()
            {
                Text = localizer[nameof(AppStrings.Home)],
                IconName = BitIconName.Home,
                Url = PageUrls.Home,
            }
        ];

        var (dashboard, manageProductCatalog) = await (authorizationService.IsAuthorizedAsync(authUser!, AppFeatures.AdminPanel.Dashboard),
            authorizationService.IsAuthorizedAsync(authUser!, AppFeatures.AdminPanel.ManageProductCatalog));

        if (dashboard || manageProductCatalog)
        {
           /* BitNavItem adminPanelItem = new()
            {
                Text = localizer[nameof(AppStrings.AdminPanel)],
                IconName = BitIconName.Admin,
                ChildItems = []
            };

            navPanelItems.Add(adminPanelItem);*/

            BitNavItem abhayyoganaPanelItem = new()
            {
                Text = localizer[nameof(AppStrings.SRAAbhayYojana)],
                IconName = BitIconName.Database,
                ChildItems = []
            };

            navPanelItems.Add(abhayyoganaPanelItem);

           /* if (dashboard)
            {
                adminPanelItem.ChildItems.Add(new()
                {
                    Text = localizer[nameof(AppStrings.Dashboard)],
                    IconName = BitIconName.BarChartVerticalFill,
                    Url = PageUrls.Dashboard,
                });
            }*/

            if (manageProductCatalog)
            {
               /* adminPanelItem.ChildItems.AddRange(
                [
                    new()
                        {
                            Text = localizer[nameof(AppStrings.Categories)],
                            IconName = BitIconName.BuildQueue,
                            Url = PageUrls.Categories,
                        },
                        new()
                        {
                            Text = localizer[nameof(AppStrings.Products)],
                            IconName = BitIconName.Product,
                            Url = PageUrls.Products,
                        }
                ]);*/

                abhayyoganaPanelItem.ChildItems.AddRange(
                [
                    new()
                        {
                            Text = localizer[nameof(AppStrings.AbhayYojana)],
                            IconName = BitIconName.XRay,
                            Url = PageUrls.AbhayYojana,
                        }
                ]);
            }
        }



       /* navPanelItems.Add(new()
        {
            Text = localizer[nameof(AppStrings.Terms)],
            IconName = BitIconName.EntityExtraction,
            Url = PageUrls.Terms,
        });

        navPanelItems.Add(new()
        {
            Text = localizer[nameof(AppStrings.About)],
            IconName = BitIconName.Info,
            Url = PageUrls.About,
        });*/

        var (manageRoles, manageUsers, manageAiPrompt) = await (authorizationService.IsAuthorizedAsync(authUser!, AppFeatures.Management.ManageRoles),
            authorizationService.IsAuthorizedAsync(authUser!, AppFeatures.Management.ManageUsers),
            authorizationService.IsAuthorizedAsync(authUser!, AppFeatures.Management.ManageAiPrompt));

     /*   if (manageRoles || manageUsers || manageAiPrompt)
        {
            BitNavItem managementItem = new()
            {
                Text = localizer[nameof(AppStrings.Management)],
                IconName = BitIconName.SettingsSecure,
                ChildItems = []
            };

            navPanelItems.Add(managementItem);

            if (manageRoles)
            {
                managementItem.ChildItems.Add(new()
                {
                    Text = localizer[nameof(AppStrings.UserGroups)],
                    IconName = BitIconName.WorkforceManagement,
                    Url = PageUrls.Roles,
                });
            }

            if (manageUsers)
            {
                managementItem.ChildItems.Add(new()
                {
                    Text = localizer[nameof(AppStrings.Users)],
                    IconName = BitIconName.SecurityGroup,
                    Url = PageUrls.Users,
                });
            }

        }*/

        if (authUser.IsAuthenticated())
        {
            navPanelItems.Add(new()
            {
                Text = localizer[nameof(AppStrings.Settings)],
                IconName = BitIconName.Equalizer,
                Url = PageUrls.Settings,
                AdditionalUrls =
                [
                    $"{PageUrls.Settings}/{PageUrls.SettingsSections.Profile}",
                    $"{PageUrls.Settings}/{PageUrls.SettingsSections.Account}",
                    $"{PageUrls.Settings}/{PageUrls.SettingsSections.Tfa}",
                    $"{PageUrls.Settings}/{PageUrls.SettingsSections.Sessions}",
                    $"{PageUrls.Settings}/{PageUrls.SettingsSections.UpgradeAccount}",
                ]
            });
        }
    }
}
