using System;
using Autofac;
using ECSDiscord.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ECSDiscord.Services.Verification;

public class VerificationModule : Module
{
    private readonly IConfiguration _configuration;

    public VerificationModule(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<VerificationService>()
            .AsSelf()
            .As<IHostedService>()
            .SingleInstance();
        builder.ConfigureWithValidation<VerificationOptions>(
            _configuration.GetExistingSectionOrThrow(VerificationOptions.Name));
        builder.AddFluentValidator<VerificationOptions, VerificationOptionsValidation>();
    }
}