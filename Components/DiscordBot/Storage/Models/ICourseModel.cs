using System;
using System.Collections.Generic;
using System.Text;

namespace ECSDiscordStorage.Models
{
    public interface ICourseModel
    {
        string Name { get; }
        long DiscordChannelSnowflake { get; }
        bool RequireVerification { get; }
        bool AutoJoin { get; }
    }
}
