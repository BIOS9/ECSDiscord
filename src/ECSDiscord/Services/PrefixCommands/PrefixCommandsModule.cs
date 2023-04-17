using System;
using Autofac;
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
        builder.RegisterType<PrefixCommandsHandler>()
            .AsSelf()
            .As<IHostedService>()
            .SingleInstance();
        builder.ConfigureWithValidation<PrefixCommandsOptions>(
            _configuration.GetExistingSectionOrThrow(PrefixCommandsOptions.Name));
    }
}