using Autofac;
using DiscordBot.Application;
using DiscordBot.Discord;
using DiscordBot.Logging;
using DiscordBot.Translation;

namespace DiscordBot
{
    internal static class ContainerConfig
    {
        public static IContainer Configure()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<DiscordBotApplication>().As<IApplication>().SingleInstance();
            builder.Register((c, p) => LoggerConfig.CreateLoggerFactory()).SingleInstance();
            builder.Register((c, p) => TranslatorConfig.CreatePluginTranslatorFactory(c)).SingleInstance();
            builder.Register((c, p) => TranslatorConfig.CreateTranslationFileFactory()).SingleInstance();
            builder.Register((c, p) => DiscordBotConfig.CreateDiscordBot()).SingleInstance();

            return builder.Build();
        }
    }
}
