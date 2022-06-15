using System.IO;

namespace ECSDiscord.Services
{
    public class ResourceReaderService
    {
        private const string BackroomsPath = "Resources/Backrooms";

        public static Stream OpenBackroomsImage(int num)
        {
            return new FileStream(Path.Join(BackroomsPath, num + ".jpeg"), FileMode.Open, FileAccess.Read);
        }
    }
}