using Autofac.Extensions.DependencyInjection;
using Autofac;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ECSDiscord.Core.Translations;
using Microsoft.Extensions.Configuration;
using Serilog;
using Microsoft.Extensions.Hosting;
using ECSDiscord;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddYamlFile("config.yml", true);
        config.AddYamlFile("/config/config.yml", true);
        config.AddEnvironmentVariables();
        config.AddUserSecrets<Program>();
    })
    .UseSerilog((context, config) => { config.ReadFrom.Configuration(context.Configuration); })
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>((context, builder) =>
    {
        builder.RegisterInstance(new DiscordSocketClient(new DiscordSocketConfig
        {                                       // Add discord to the collection
            LogLevel = LogSeverity.Info,     // Tell the logger to give Verbose amount of info
            MessageCacheSize = 1000             // Cache 1,000 messages per channel
        })).SingleInstance();

        builder.RegisterInstance(new Discord.Commands.CommandService(new CommandServiceConfig
        {                                       // Add the command service to the collection
            LogLevel = LogSeverity.Verbose,     // Tell the logger to give Verbose amount of info
            DefaultRunMode = RunMode.Async,     // Force all commands to run async by default
        })).SingleInstance();

        // all very ugly right now, will clean soon
        builder.RegisterInstance((ITranslator)Translator.DefaultTranslations).SingleInstance();
        builder.RegisterType<ECSDiscord.Services.CommandService>().AsSelf().As<IHostedService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.StartupService>().AsSelf().As<IHostedService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.LoggingService>().AsSelf().As<IHostedService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.EnrollmentsService>().AsSelf().As<IHostedService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.CourseService>().AsSelf().As<IHostedService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.StorageService>().AsSelf().As<IHostedService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.VerificationService>().AsSelf().As<IHostedService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.RemoteDataAccessService>().AsSelf().As<IHostedService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.AdministrationService>().AsSelf().As<IHostedService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.ServerMessageService>().AsSelf().As<IHostedService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.TransientStateService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.ImportService>().SingleInstance();
    })
    .Build()
    .RunAsync();