using System;
using System.Collections.Generic;
using System.Text;

namespace ECSDiscordStorage.Models
{
    public interface IPendingVerificationModel
    {
        string Token { get; }
        Span<byte> EncryptedUsername { get; }
        long DiscordSnowflake { get; }
        long CreationTime { get; }
    }
}
