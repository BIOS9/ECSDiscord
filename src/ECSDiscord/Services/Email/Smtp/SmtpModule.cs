using System;
using Autofac;
using ECSDiscord.Util;
using Microsoft.Extensions.Configuration;

namespace ECSDiscord.Services.Email.Smtp;

internal class SmtpModule : Module
{
    private readonly IConfiguration _configuration;

    public SmtpModule(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SmtpMailSender>()
            .As<IMailSender>();
        builder.ConfigureWithValidation<SmtpOptions>(
            _configuration.GetExistingSectionOrThrow(SmtpOptions.Name));
    }
}