using System.Threading.Tasks;

namespace ComponentApplication.Components.Services
{
    public interface IServiceManager
    {
        void DeregisterService(IService service);
        void RegisterService(IService service);
        Task StartServices();
        Task StopServices();
    }
}