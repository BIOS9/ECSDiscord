using ECSDiscord.Services;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ECSDiscord
{
    internal class Startup : IHostedService
    {
        public required CommandService CommandService { protected get; init; }
        public required LoggingService LoggingService { protected get; init; }
        public required EnrollmentsService EnrollmentsService { protected get; init; }
        public required CourseService CourseService { protected get; init; }
        public required StorageService StorageService { protected get; init; }
        public required VerificationService VerificationService { protected get; init; }
        public required RemoteDataAccessService RemoteDataAccessService { protected get; init; }
        public required ImportService ImportService { protected get; init; }
        public required AdministrationService AdministrationService { protected get; init; }
        public required ServerMessageService ServerMessageService { protected get; init; }
        public required StartupService StartupService { protected get; init; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            
            if (!await StorageService.TestConnection()) // Test DB connection
                throw new Exception("Storage service init failed.");

            await StartupService.StartAsync(); // Run startup service
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
