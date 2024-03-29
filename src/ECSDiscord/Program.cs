﻿using Autofac;
using Autofac.Extensions.DependencyInjection;
using ECSDiscord.Services.Bot;
using ECSDiscord.Services.Courses;
using ECSDiscord.Services.Email.Sendgrid;
using ECSDiscord.Services.Enrollments;
using ECSDiscord.Services.Modals;
using ECSDiscord.Services.ModerationLog;
using ECSDiscord.Services.PrefixCommands;
using ECSDiscord.Services.ServerMessages;
using ECSDiscord.Services.SlashCommands;
using ECSDiscord.Services.Storage;
using ECSDiscord.Services.Translations;
using ECSDiscord.Services.Verification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

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
        builder.RegisterModule(new StorageModule(context.Configuration));
        builder.RegisterModule(new SendGridModule(context.Configuration));
        builder.RegisterModule(new EnrollmentsModule(context.Configuration));
        builder.RegisterModule(new CoursesModule(context.Configuration));
        builder.RegisterModule(new VerificationModule(context.Configuration));
        builder.RegisterModule(new PrefixCommandsModule(context.Configuration));
        builder.RegisterModule<ModalsModule>();
        builder.RegisterModule<SlashCommandsModule>();
        builder.RegisterModule<ServerMessagesModule>();
        builder.RegisterModule<TranslationsModule>();
        builder.RegisterModule<ModerationLogModule>();
        builder.RegisterModule(new BotModule(context.Configuration));
    })
    .Build()
    .RunAsync();