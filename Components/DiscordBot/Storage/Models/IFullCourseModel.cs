using System;
using System.Collections.Generic;
using System.Text;

namespace ECSDiscordStorage.Models
{
    public interface IFullCourseModel : ICourseModel
    {
        IEnumerable<IUserModel> Users { get; }
    }
}
