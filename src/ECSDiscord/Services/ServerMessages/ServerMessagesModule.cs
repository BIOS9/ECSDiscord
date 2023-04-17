using Autofac;
using Microsoft.Extensions.Hosting;

namespace ECSDiscord.Services.ServerMessages;

public class ServerMessagesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ServerMessageService>()
            .AsSelf()
            .As<IHostedService>()
            .SingleInstance();
    }
}