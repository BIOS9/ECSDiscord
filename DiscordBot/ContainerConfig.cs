using Autofac;

namespace DiscordBot
{
    internal static class ContainerConfig
    {
        public static IContainer Configure()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<DiscordBot>().As<IApplication>().SingleInstance();
            builder.Register((c, p) => LoggerConfig.CreateLoggerFactory()).SingleInstance();
            builder.Register((c, p) => Translation.TranslatorConfig.CreateTranslatorFactory()).SingleInstance();
            return builder.Build();
        }
    }
}
