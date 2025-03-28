using Autofac;
using Microsoft.Extensions.Hosting;

namespace ECSDiscord.Services.Minecraft;

public class MinecraftModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MinecraftService>()
            .AsSelf()
            .As<IHostedService>()
            .SingleInstance();
    }
}