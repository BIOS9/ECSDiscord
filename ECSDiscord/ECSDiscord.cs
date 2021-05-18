using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ECSDiscord.Core.Translations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ECSDiscord
{
    /// <summary>
    /// Main application class.
    /// </summary>
    public class ECSDiscord
    {
        public const string
            ConfigurationFile = "config.yml",
            LogFileName = "logs/log.txt";
        public const RollingInterval
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
        public async Task RunStandaloneAsync()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            await StartAsync(services.BuildServiceProvider());
            await Task.Delay(-1); // Keep program from exiting
        }

        public async Task StartAsync(IServiceProvider serviceProvider)
        {
            if (!checkConfig())
            {
                Log.Fatal("Invalid configuration. Exiting...");
                throw new Exception("Invalid config");
            }

            serviceProvider.GetRequiredService<Services.CommandService>(); // Start command handler service
            serviceProvider.GetRequiredService<Services.LoggingService>(); // Start logging service
            serviceProvider.GetRequiredService<Services.EnrollmentsService>(); // Start enrollments service
            serviceProvider.GetRequiredService<Services.CourseService>(); // Start course service
            serviceProvider.GetRequiredService<Services.StorageService>(); // Start course service
            serviceProvider.GetRequiredService<Services.VerificationService>(); // Start verification service
            serviceProvider.GetRequiredService<Services.RemoteDataAccessService>(); // Start remote data access service
            serviceProvider.GetRequiredService<Services.ImportService>(); // Start import service
            serviceProvider.GetRequiredService<Services.AdministrationService>(); // Start import service
            if (!await serviceProvider.GetRequiredService<Services.StorageService>().TestConnection()) // Test DB connection
                throw new Exception("Storage service init failed.");
            await serviceProvider.GetRequiredService<Services.StartupService>().StartAsync(); // Run startup service
        }

        /// <summary>
        /// Configure services and add required shared objects.
        /// </summary>
        public IServiceCollection ConfigureServices(IServiceCollection services)
        {
            return services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
            {                                       // Add discord to the collection
                LogLevel = LogSeverity.Info,     // Tell the logger to give Verbose amount of info
                MessageCacheSize = 1000             // Cache 1,000 messages per channel
            }))
            .AddSingleton(new Discord.Commands.CommandService(new CommandServiceConfig
            {                                       // Add the command service to the collection
                LogLevel = LogSeverity.Verbose,     // Tell the logger to give Verbose amount of info
                DefaultRunMode = RunMode.Async,     // Force all commands to run async by default
            }))
            .AddSingleton((ITranslator)Translator.DefaultTranslations)       // Add Translations provider
            .AddSingleton<Services.CommandService>()         // Add commandservice to the collection
            .AddSingleton<Services.StartupService>()         // Add startupservice to the collection
            .AddSingleton<Services.LoggingService>()         // Add loggingservice to the collection
            .AddSingleton<Services.EnrollmentsService>()     // Add enrollmentsservice to the collection
            .AddSingleton<Services.CourseService>()          // Add courseservice to the collection
            .AddSingleton<Services.StorageService>()          // Add storageservice to the collection
            .AddSingleton<Services.VerificationService>()       // Add verificationservice to the collection
            .AddSingleton<Services.RemoteDataAccessService>()       // Add verificationservice to the collection
            .AddSingleton<Services.ImportService>()             // Add import service
            .AddSingleton<Services.AdministrationService>()             // Add import service
            .AddSingleton(this)
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

            return new ECSDiscord().RunStandaloneAsync();
        }
    }
}
