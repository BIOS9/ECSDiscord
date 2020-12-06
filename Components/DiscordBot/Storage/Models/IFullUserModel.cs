using System;
using System.Collections.Generic;
using System.Text;

namespace ECSDiscordStorage.Models
{
    public interface IFullUserModel : IUserModel
    {
        public interface IUsernameRecord
        {
            Span<byte> EncryptedUsername { get; }
            long VerificationTime { get; }
        }

        IEnumerable<ICourseModel> Courses { get; }
        IEnumerable<IUsernameRecord> UsernameHistory { get; }
    }
}
