using Autofac.Extensions.DependencyInjection;
using Autofac;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ECSDiscord.Core.Translations;
using Microsoft.Extensions.Configuration;
using Serilog;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
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


        builder.RegisterInstance((ITranslator)Translator.DefaultTranslations).SingleInstance();
        builder.RegisterType<ECSDiscord.Services.CommandService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.StartupService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.LoggingService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.EnrollmentsService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.CourseService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.StorageService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.VerificationService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.RemoteDataAccessService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.TransientStateService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.ImportService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.AdministrationService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.ServerMessageService>().SingleInstance();
    })
    .Build()
    .RunAsync();


        //public async Task StartAsync(IServiceProvider serviceProvider)
        //{

        //    serviceProvider.GetRequiredService<Services.CommandService>(); // Start command handler service
        //    serviceProvider.GetRequiredService<Services.LoggingService>(); // Start logging service
        //    serviceProvider.GetRequiredService<Services.EnrollmentsService>(); // Start enrollments service
        //    serviceProvider.GetRequiredService<Services.CourseService>(); // Start course service
        //    serviceProvider.GetRequiredService<Services.StorageService>(); // Start course service
        //    serviceProvider.GetRequiredService<Services.VerificationService>(); // Start verification service
        //    serviceProvider.GetRequiredService<Services.RemoteDataAccessService>(); // Start remote data access service
        //    serviceProvider.GetRequiredService<Services.ImportService>(); // Start import service
        //    serviceProvider.GetRequiredService<Services.AdministrationService>(); // Start import service
        //    serviceProvider.GetRequiredService<Services.ServerMessageService>(); // Start message service
        //    if (!await serviceProvider.GetRequiredService<Services.StorageService>().TestConnection()) // Test DB connection
        //        throw new Exception("Storage service init failed.");
        //    await serviceProvider.GetRequiredService<Services.StartupService>().StartAsync(); // Run startup service
        //}