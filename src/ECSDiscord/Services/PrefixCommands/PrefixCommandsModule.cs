using System;
using Autofac;
using Discord;
using Discord.Commands;
using ECSDiscord.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ECSDiscord.Services.PrefixCommands;

public class PrefixCommandsModule : Module
{
    private readonly IConfiguration _configuration;

    public PrefixCommandsModule(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterInstance(new CommandService(new CommandServiceConfig
        {                                       // Add the command service to the collection
            LogLevel = LogSeverity.Verbose,     // Tell the logger to give Verbose amount of info
            DefaultRunMode = RunMode.Async,     // Force all commands to run async by default
        })).SingleInstance();
        builder.RegisterType<PrefixCommandsHandler>()
            .AsSelf()
            .As<IHostedService>()
            .SingleInstance();
        builder.ConfigureWithValidation<PrefixCommandsOptions>(
            _configuration.GetExistingSectionOrThrow(PrefixCommandsOptions.Name));
    }
}