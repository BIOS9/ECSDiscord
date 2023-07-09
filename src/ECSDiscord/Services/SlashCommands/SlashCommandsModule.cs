using Autofac;
using ECSDiscord.Services.SlashCommands.Commands;
using Microsoft.Extensions.Hosting;

namespace ECSDiscord.Services.SlashCommands;

public class SlashCommandsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SlashCommandsHandler>()
            .AsSelf()
            .As<IHostedService>()
            .SingleInstance();
        builder.RegisterType<VerifyCommand>().As<ISlashCommand>();
        builder.RegisterType<JoinCommand>().As<ISlashCommand>();
        builder.RegisterType<LeaveCommand>().As<ISlashCommand>();
        builder.RegisterType<LeaveAllCommand>().As<ISlashCommand>();
        builder.RegisterType<ListCoursesCommand>().As<ISlashCommand>();
        builder.RegisterType<MyCoursesCommand>().As<ISlashCommand>();
        builder.RegisterType<BotMessagesCommand>().As<ISlashCommand>();
        builder.RegisterType<ResetCourseCommand>().As<ISlashCommand>();
    }
}