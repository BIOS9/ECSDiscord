using System.Threading.Tasks;

namespace ComponentApplication.Components.Services
{
    /// <summary>
    /// Manages state of application services.
    /// </summary>
    public interface IServiceManager
    {
        /// <summary>
        /// Register service for management in this manager.
        /// </summary>
        /// <param name="service">Service to be managed.</param>
        void RegisterService(IService service);

        /// <summary>
        /// Deregister service for management in this manager.
        /// </summary>
        /// <param name="service">Service being managed.</param>
        void DeregisterService(IService service);

        /// <summary>
        /// Start all services.
        /// </summary>
        /// <returns>Task waiting for all services to start.</returns>
        Task StartServices();

        /// <summary>
        /// Stop all services.
        /// </summary>
        /// <returns>Task waiting for all services to stop.</returns>
        void StopServices();
    }
}