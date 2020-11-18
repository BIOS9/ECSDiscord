using Autofac;
using ComponentApplication.Components;
using ComponentApplication.Components.Services;
using System.Reflection;

namespace ComponentApplication
{
    /// <summary>
    /// Dependency injection setup/configuration.
    /// </summary>
    internal static class ContainerConfig
    {
        /// <summary>
        /// Configure dependency injection.
        /// </summary>
        /// <param name="loader">Component loader that was used to load component assemblies.</param>
        /// <param name="components">Assemblies containing components to be registered.</param>
        /// <returns>Dependency container.</returns>
        public static IContainer Configure(IComponentLoader loader, Assembly[] components)
        {
            var builder = new ContainerBuilder();
            // Register components from loaded assemblies.
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
