using Autofac.Extensions.DependencyInjection;
using Autofac;
using Microsoft.Extensions.Configuration;
using Serilog;
using Microsoft.Extensions.Hosting;
using ECSDiscord.Services.SlashCommands;
using ECSDiscord.Services.Bot;
using ECSDiscord.Services.Email.Sendgrid;
using ECSDiscord.Services.Enrollments;
using ECSDiscord.Services.PrefixCommands;
using ECSDiscord.Services.Translations;
using ECSDiscord.Services.Verification;

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
        // all very ugly right now, will clean soon
        builder.RegisterType<ECSDiscord.Services.CourseService>().AsSelf().As<IHostedService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.StorageService>().AsSelf().As<IHostedService>().SingleInstance();
        builder.RegisterType<ECSDiscord.Services.ServerMessageService>().AsSelf().As<IHostedService>().SingleInstance();

        builder.RegisterModule(new BotModule(context.Configuration));
        builder.RegisterModule(new SendGridModule(context.Configuration));
        builder.RegisterModule(new EnrollmentsModule(context.Configuration));
        builder.RegisterModule(new VerificationModule(context.Configuration));
        builder.RegisterModule(new PrefixCommandsModule(context.Configuration));
        builder.RegisterModule<SlashCommandsModule>();
        builder.RegisterModule<TranslationsModule>();
    })
    .Build()
    .RunAsync();