using System.Threading.Tasks;

namespace ComponentApplication.Components.Services
{
    /// <summary>
    /// Represents an abstract service (aka runnable component).
    /// </summary>
    public interface IService : IComponent
    {
        public enum ServiceState
        {
            Stopped,
            Starting,
            Running,
            Stopping
        }

        /// <summary>
        /// Life state of service.
        /// Handled internally by each service.
        /// </summary>
        ServiceState State { get; }

        /// <summary>
        /// Starts service.
        /// </summary>
        /// <returns>Task waiting for completion of service execution.</returns>
        Task StartAsync();

        /// <summary>
        /// Stops service.
        /// </summary>
        /// <returns>Task waiting for completion of shutdown tasks.</returns>
        Task StopAsync();
    }
}