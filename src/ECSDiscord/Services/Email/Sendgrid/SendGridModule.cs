using Autofac;
using Microsoft.Extensions.Configuration;
using System;
using ECSDiscord.Util;

namespace ECSDiscord.Services.Email.Sendgrid;

internal class SendGridModule : Module
{
    private readonly IConfiguration _configuration;

    public SendGridModule(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SendGridMailSender>()
            .As<IMailSender>();
        builder.ConfigureWithValidation<SendGridOptions>(
            _configuration.GetExistingSectionOrThrow(SendGridOptions.Name));
    }
}
