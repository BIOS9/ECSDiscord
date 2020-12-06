using System;
using System.Collections.Generic;
using System.Text;

namespace ECSDiscordStorage.Models
{
    public interface IUserModel
    {
        long DiscordSnowflake { get; } 
        Span<byte> EncryptedUsername { get; }
        bool IsVerified { get; }
    }
}
