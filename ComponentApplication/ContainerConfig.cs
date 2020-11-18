using Autofac;
using ComponentApplication.Components;
using ComponentApplication.Components.Services;
using System.Reflection;

namespace ComponentApplication
{
    internal static class ContainerConfig
    {
        public static IContainer Configure(IComponentLoader loader, Assembly[] components)
        {
            var builder = new ContainerBuilder();
            builder.RegisterAssemblyTypes(components)
                .AssignableTo<IComponent>()
                .As<IComponent>()
                .AsSelf()
                .AsImplementedInterfaces();
            builder.Register((c, t) => loader);
            builder.RegisterType<ServiceManager>().As<IServiceManager>();
            return builder.Build();
        }
    }
}
