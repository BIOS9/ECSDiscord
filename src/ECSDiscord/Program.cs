using System.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using ECSDiscord.Services;
using ECSDiscord.Services.Bot;
using ECSDiscord.Services.Courses;
using ECSDiscord.Services.Email.Sendgrid;
using ECSDiscord.Services.Enrollments;
using ECSDiscord.Services.Minecraft;
using ECSDiscord.Services.Modals;
using ECSDiscord.Services.ModerationLog;
using ECSDiscord.Services.PrefixCommands;
using ECSDiscord.Services.ServerMessages;
using ECSDiscord.Services.SlashCommands;
using ECSDiscord.Services.Storage;
using ECSDiscord.Services.Translations;
using ECSDiscord.Services.Verification;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureAppConfiguration(config =>
    {
        config.AddYamlFile("config.yml", true);
        config.AddYamlFile("/config/config.yml", true);
        config.AddEnvironmentVariables();
        config.AddUserSecrets<Program>();
    })
    .UseSerilog((context, config) =>
    {
        config.ReadFrom.Configuration(context.Configuration);
    })
    .ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
    {
        containerBuilder.RegisterModule(new StorageModule(context.Configuration));
        containerBuilder.RegisterModule(new SendGridModule(context.Configuration));
        containerBuilder.RegisterModule(new EnrollmentsModule(context.Configuration));
        containerBuilder.RegisterModule(new CoursesModule(context.Configuration));
        containerBuilder.RegisterModule(new VerificationModule(context.Configuration));
        containerBuilder.RegisterModule(new PrefixCommandsModule(context.Configuration));
        containerBuilder.RegisterModule<ModalsModule>();
        containerBuilder.RegisterModule<SlashCommandsModule>();
        containerBuilder.RegisterModule<ServerMessagesModule>();
        containerBuilder.RegisterModule<TranslationsModule>();
        containerBuilder.RegisterModule<ModerationLogModule>();
        containerBuilder.RegisterModule<MinecraftModule>();
        containerBuilder.RegisterModule(new BotModule(context.Configuration));
    });
builder.Services.AddControllers();

var app = builder.Build();
app.UseRouting();
app.MapControllers();

await app.RunAsync();
