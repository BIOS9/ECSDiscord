using System;
using Autofac;
using ECSDiscord.Services.Verification;
using ECSDiscord.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ECSDiscord.Services.Storage;

public class StorageModule : Module
{
    private readonly IConfiguration _configuration;

    public StorageModule(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<StorageService>()
            .AsSelf()
            .As<IHostedService>()
            .SingleInstance();
        builder.ConfigureWithValidation<StorageOptions>(
            _configuration.GetExistingSectionOrThrow(StorageOptions.Name));
    }
}