namespace ECSDiscordStorage.Models
{
    public interface IVerificationOverrideModel
    {
        public enum ObjectType
        {
            ROLE,
            USER
        }

        long DiscordSnowflake { get; }
        ObjectType Type { get; }
    }
}
