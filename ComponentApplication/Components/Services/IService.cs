using System.Threading;
using System.Threading.Tasks;

namespace ComponentApplication.Components.Services
{
    /// <summary>
    /// Represents an abstract service (aka runnable component).
    /// </summary>
    public interface IService : IComponent
    {
        /// <summary>
        /// Starts service.
        /// </summary>
        /// <returns>Task waiting for completion of service startup.</returns>
        Task StartAsync();

        /// <summary>
        /// Run main service functionality.
        /// Waits for completion of service execution.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken to end service execution.</param>
        /// <returns>Task waiting for service execution to finish.</returns>
        Task RunAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stops service.
        /// </summary>
        /// <returns>Task waiting for completion of shutdown tasks.</returns>
        Task StopAsync();
    }
}