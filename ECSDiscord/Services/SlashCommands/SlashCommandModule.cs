using Autofac;
using Microsoft.Extensions.Hosting;

namespace ECSDiscord.Services.SlashCommands;

public class SlashCommandsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SlashCommandHandler>()
            .AsSelf()
            .As<IHostedService>()
            .SingleInstance();
    }
}