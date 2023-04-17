using Autofac.Extensions.DependencyInjection;
using Autofac;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Serilog;
using Microsoft.Extensions.Hosting;
using ECSDiscord.Services.SlashCommands;
using ECSDiscord.Services.Bot;
using ECSDiscord.Services.Email.Sendgrid;
using ECSDiscord.Services.Enrollments;
using ECSDiscord.Services.PrefixCommands;
using ECSDiscord.Services.Translations;

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
        builder.RegisterInstance(new CommandService(new CommandServiceConfig
        {                                       // Add the command service to the collection
            LogLevel = LogSeverity.Verbose,     // Tell the logger to give Verbose amount of info
            DefaultRunMode = RunMode.Async,     // Force all commands to run async by default
        })).SingleInstance();

        // all very ugly right now, will clean soon
        builder.RegisterType<ECSDiscord.Services.CourseService>().AsSelf().As<IHostedService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.StorageService>().AsSelf().As<IHostedService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.VerificationService>().AsSelf().As<IHostedService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.ServerMessageService>().AsSelf().As<IHostedService>().SingleInstance();

        builder.RegisterModule(new BotModule(context.Configuration));
        builder.RegisterModule(new SendGridModule(context.Configuration));
        builder.RegisterModule(new EnrollmentsModule(context.Configuration));
        builder.RegisterModule<SlashCommandsModule>();
        builder.RegisterModule<PrefixCommandsModule>();
        builder.RegisterModule<TranslationsModule>();
    })
    .Build()
    .RunAsync();