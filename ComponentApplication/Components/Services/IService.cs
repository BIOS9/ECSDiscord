using System;
using System.Threading.Tasks;

namespace ComponentApplication.Components.Services
{
    public interface IService : IComponent
    {
        public enum ServiceState
        {
            Stopped,
            Starting,
            Running,
            Stopping
        }
        ServiceState State { get; }
        Task StartAsync();
        Task StopAsync();
    }
}