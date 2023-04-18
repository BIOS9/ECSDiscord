using Autofac;
using Microsoft.Extensions.Hosting;

namespace ECSDiscord.Services.Modals;

public class ModalsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ModalsHandler>()
            .AsSelf()
            .As<IHostedService>()
            .SingleInstance();
    }
}