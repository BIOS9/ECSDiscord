using IdentityModel;
using IdentityServer4.Models;
using System.Collections.Generic;
using static IdentityServer4.IdentityServerConstants;

namespace ECSWebDashboard
{
    public class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources => new List<IdentityResource>
        {
            new IdentityResources.OpenId(),
            new IdentityResource("profile", Discord.OAuth2.DiscordDefaults.Claims)
            //new IdentityResources.Profile(),
        };

        public static IEnumerable<ApiResource> ApiResources => new List<ApiResource>
        {
            new ApiResource("ecsdiscord")
            {
                UserClaims =
                {
                    JwtClaimTypes.Audience
                },
                Scopes = new List<string>
                {
                    "ecsdiscord"
                }
            }
        };

        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            {
                new ApiScope("ecsdiscord"),
                new ApiScope(StandardScopes.OfflineAccess)
            };

        public static IEnumerable<Client> Clients => new List<Client>
        {
            // interactive client using code flow + pkce
            new Client
            {
                ClientId = "public-dashboard",
#if DEBUG
                AccessTokenLifetime = 43200,
#else
                AccessTokenLifetime = 120, // Access token needs refreshed every 120s
#endif
                RefreshTokenUsage = TokenUsage.OneTimeOnly,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                SlidingRefreshTokenLifetime = 1800, // If app is closed for half an hour, logout
                AbsoluteRefreshTokenLifetime = 43200, // Log out after 12 hours no matter what.
                AllowOfflineAccess = true,

                RequireClientSecret = false,
                RequirePkce = true,
                //ClientSecrets = { new Secret("49C1A7E1-0C79-4A89-A3D6-A37998FB86B0".Sha256()) },
                    
                AllowedGrantTypes = GrantTypes.Code,

#if DEBUG
                RedirectUris = { "https://localhost:44300/signin-oidc", "http://localhost:3000", "https://oauth.pstmn.io/v1/callback" },
                FrontChannelLogoutUri = "https://localhost:44300/signout-oidc",
                PostLogoutRedirectUris = { "https://localhost:44300/signout-callback-oidc" },
#endif

                AllowedScopes = { "openid", "profile", "ecsdiscord", StandardScopes.OfflineAccess },

                AllowedCorsOrigins = { "http://localhost:3000" },

                AlwaysIncludeUserClaimsInIdToken = true,
                AlwaysSendClientClaims = true
            },
        };
    }
}
