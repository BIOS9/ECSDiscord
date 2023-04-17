using System;
using Autofac;
using ECSDiscord.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ECSDiscord.Services.Enrollments;

public class EnrollmentsModule : Module
{
    private readonly IConfiguration _configuration;

    public EnrollmentsModule(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<EnrollmentsService>()
            .AsSelf()
            .As<IHostedService>()
            .SingleInstance();
        builder.ConfigureWithValidation<EnrollmentsOptions>(
            _configuration.GetExistingSectionOrThrow(EnrollmentsOptions.Name));
    }
}