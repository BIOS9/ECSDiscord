using Autofac;
using Microsoft.Extensions.Hosting;

namespace ECSDiscord.Services.PrefixCommands;

public class PrefixCommandsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<PrefixCommandsHandler>()
            .AsSelf()
            .As<IHostedService>()
            .SingleInstance();
    }
}