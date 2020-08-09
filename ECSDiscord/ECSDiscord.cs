using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;
using System.Threading.Tasks;

namespace ECSDiscord
{
    /// <summary>
    /// Main application class.
    /// </summary>
    class ECSDiscord
    {
        private const string
            ConfigurationFile = "config.yml",
            LogFileName = "logs/log.txt";
        private const RollingInterval
            LogInterval = RollingInterval.Day;

        public IConfigurationRoot Configuration { get; }

        public ECSDiscord()
        {
            if (!File.Exists(ConfigurationFile)) // Check that config file exists
            {
                Log.Fatal("Cannot find configuration file: \"{configurationFile}\" Exiting...", ConfigurationFile);
                throw new FileNotFoundException("Cannot find config file.", ConfigurationFile);
            }

            // Add configuration from yaml file.
            Configuration = new ConfigurationBuilder()
                .AddYamlFile(ConfigurationFile)
                .Build();
        }

        private bool checkConfig()
        {
            if (string.IsNullOrWhiteSpace(Configuration["guildId"]) || !ulong.TryParse(Configuration["guildId"], out _))
            {
                Log.Error("Invalid guildId in config. Please configure the Discord guild ID of the server.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Start application.
        /// </summary>
        public async Task RunAsync()
        {
            if (!checkConfig())
            {
                Log.Fatal("Invalid configuration. Exiting...");
                return;
            }

            var services = new ServiceCollection();
            configureServices(services);

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<Services.CommandService>(); // Start command handler service
            provider.GetRequiredService<Services.LoggingService>(); // Start logging service
            provider.GetRequiredService<Services.EnrollmentsService>(); // Start enrollments service
            provider.GetRequiredService<Services.CourseService>(); // Start course service
            provider.GetRequiredService<Services.StorageService>(); // Start course service
            provider.GetRequiredService<Services.VerificationService>(); // Start verification service
            provider.GetRequiredService<Services.RemoteDataAccessService>(); // Start remote data access service
            await provider.GetRequiredService<Services.StartupService>().StartAsync(); // Run startup service
            if (await provider.GetRequiredService<Services.StorageService>().TestConnection()) // Test DB connection
                await Task.Delay(-1); // Keep program from exiting
        }

        /// <summary>
        /// Configure services and add required shared objects.
        /// </summary>
        private void configureServices(IServiceCollection services)
        {
            services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
            {                                       // Add discord to the collection
                LogLevel = LogSeverity.Info,     // Tell the logger to give Verbose amount of info
                MessageCacheSize = 1000             // Cache 1,000 messages per channel
            }))
            .AddSingleton(new Discord.Commands.CommandService(new CommandServiceConfig
            {                                       // Add the command service to the collection
                LogLevel = LogSeverity.Verbose,     // Tell the logger to give Verbose amount of info
                DefaultRunMode = RunMode.Async,     // Force all commands to run async by default
            }))
            .AddSingleton<Services.CommandService>()         // Add commandservice to the collection
            .AddSingleton<Services.StartupService>()         // Add startupservice to the collection
            .AddSingleton<Services.LoggingService>()         // Add loggingservice to the collection
            .AddSingleton<Services.EnrollmentsService>()     // Add enrollmentsservice to the collection
            .AddSingleton<Services.CourseService>()          // Add courseservice to the collection
            .AddSingleton<Services.StorageService>()          // Add storageservice to the collection
            .AddSingleton<Services.VerificationService>()       // Add verificationservice to the collection
            .AddSingleton<Services.RemoteDataAccessService>()       // Add verificationservice to the collection
            .AddSingleton(Configuration);           // Add the configuration to the collection
        }

        static Task Main(string[] args)
        {
            // Configure logger.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(LogFileName, rollingInterval: LogInterval)
                .CreateLogger();

            return new ECSDiscord().RunAsync();
        }
    }
}
