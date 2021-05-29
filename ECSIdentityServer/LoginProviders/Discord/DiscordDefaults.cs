namespace Discord.OAuth2
{
    public static class DiscordDefaults
    {
        public const string AuthenticationScheme = "Discord";
        public const string DisplayName = "Discord";

        public static readonly string AuthorizationEndpoint = "https://discordapp.com/api/oauth2/authorize";
        public static readonly string TokenEndpoint = "https://discordapp.com/api/oauth2/token";
        public static readonly string UserInformationEndpoint = "https://discordapp.com/api/users/@me";

        public static readonly string[] Claims = new string[] {
            "discord:username",
            "discord:id",
            "discord:discriminator",
            "discord:avatar",
            "discord:mfa_enabled",
        };
    }
}
