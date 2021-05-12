using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Discord.OAuth2
{
    /// <summary> Configuration options for <see cref="DiscordHandler"/>. </summary>
    public class DiscordOptions : OAuthOptions
    {
        /// <summary> Initializes a new <see cref="DiscordOptions"/>. </summary>
        public DiscordOptions()
        {
            CallbackPath = new PathString("/signin-discord");
            AuthorizationEndpoint = DiscordDefaults.AuthorizationEndpoint;
            TokenEndpoint = DiscordDefaults.TokenEndpoint;
            UserInformationEndpoint = DiscordDefaults.UserInformationEndpoint;
            Scope.Add("identify");

            ClaimActions.MapJsonKey("discord:username", "username", ClaimValueTypes.String);
            ClaimActions.MapJsonKey("discord:discriminator", "discriminator", ClaimValueTypes.UInteger32);
            ClaimActions.MapJsonKey("discord:id", "id", ClaimValueTypes.UInteger64);
            ClaimActions.MapJsonKey("discord:avatar", "avatar", ClaimValueTypes.String);
            ClaimActions.MapJsonKey("discord:mfa_enabled", "mfa_enabled", ClaimValueTypes.Boolean);
        }
        
        /// <summary> Gets or sets the Discord-assigned appId. </summary>
        public string AppId { get => ClientId; set => ClientId = value; }        
        /// <summary> Gets or sets the Discord-assigned app secret. </summary>
        public string AppSecret { get => ClientSecret; set => ClientSecret = value; }
    }
}
