using Autofac;
using ComponentApplication.Components;
using ComponentApplication.Components.Resources;
using ComponentApplication.Components.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Context;
using System.Runtime.Loader;

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
        public static IContainer Configure(Assembly[] components)
        {

            var builder = new ContainerBuilder();
            // Register components from loaded assemblies.
            builder.RegisterAssemblyTypes(components)
                .AssignableTo<IComponent>()
                .AsSelf()
                .AsImplementedInterfaces();

            builder.RegisterType<ServiceManager>().As<IServiceManager>().SingleInstance();
            return builder.Build();
        }
    }
}
