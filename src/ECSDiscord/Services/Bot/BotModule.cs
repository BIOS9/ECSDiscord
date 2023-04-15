using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;

namespace ECSDiscord.Services.Bot
{
    internal class BotModule : Module
    {
        private readonly IConfiguration _configuration;

        public BotModule(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DiscordBot>()
                .AsSelf()
                .As<IHostedService>()
                .SingleInstance();
        }
    }
}
