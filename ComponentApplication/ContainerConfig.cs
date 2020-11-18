using Autofac;
using ComponentApplication.Components;
using System.Reflection;

namespace ComponentApplication
{
    internal static class ContainerConfig
    {
        public static IContainer Configure(Assembly[] components)
        {
            var builder = new ContainerBuilder();
            builder.RegisterAssemblyTypes(components)
                .AssignableTo<IComponent>()
                .As<IComponent>()
                .AsSelf()
                .AsImplementedInterfaces()
                .SingleInstance();
            //builder.Register((c, p) => LoggerConfig.CreateLoggerFactory()).SingleInstance();
            //builder.Register((c, p) => TranslatorConfig.CreatePluginTranslatorFactory(c)).SingleInstance();
            //builder.Register((c, p) => TranslatorConfig.CreateTranslationFileFactory()).SingleInstance();
            return builder.Build();
        }
    }
}
