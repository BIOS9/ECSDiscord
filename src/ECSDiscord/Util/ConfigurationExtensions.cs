using System;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ECSDiscord.Util;

public static class ConfigurationHelpers
{
    public static IConfigurationSection GetExistingSectionOrThrow(this IConfiguration configuration, string key)
    {
        var configurationSection = configuration.GetSection(key);

        if (!configurationSection.Exists())
        {
            throw configuration switch
            {
                IConfigurationRoot configurationIsRoot => new ArgumentException(
                    $"Section with key '{key}' does not exist. Existing values are: {configurationIsRoot.GetDebugView()}",
                    nameof(key)),
                IConfigurationSection configurationIsSection => new ArgumentException(
                    $"Section with key '{key}' does not exist at '{configurationIsSection.Path}'. Expected configuration path is '{configurationSection.Path}'",
                    nameof(key)),
                _ => new ArgumentException($"Failed to find configuration at '{configurationSection.Path}'",
                    nameof(key))
            };
        }

        return configurationSection;
    }

    public static ContainerBuilder ConfigureWithValidation<TOptions>(this ContainerBuilder builder,
        IConfiguration config) where TOptions : class
    {
        return builder.ConfigureWithValidation<TOptions>(Options.DefaultName, config);
    }

    public static ContainerBuilder ConfigureWithValidation<TOptions>(this ContainerBuilder builder, string name,
        IConfiguration config) where TOptions : class
    {
        _ = config ?? throw new ArgumentNullException(nameof(config));

        builder.RegisterInstance<IOptionsChangeTokenSource<TOptions>>(new ConfigurationChangeTokenSource<TOptions>(name, config))
            .SingleInstance();
        builder.RegisterInstance<IConfigureOptions<TOptions>>(new NamedConfigureFromConfigurationOptions<TOptions>(name, config, _ => { }))
            .SingleInstance();
        builder.AddDataAnnotationValidatedOptions<TOptions>(name);
        return builder;
    }

    public static ContainerBuilder ConfigureWithValidation<TOptions>(this ContainerBuilder builder,
        Action<TOptions> configureOptions) where TOptions : class
    {
        return builder.ConfigureWithValidation(Options.DefaultName, configureOptions);
    }

    public static ContainerBuilder ConfigureWithValidation<TOptions>(this ContainerBuilder builder, string name,
        Action<TOptions> configureOptions) where TOptions : class
    {
        builder.RegisterInstance<IConfigureOptions<TOptions>>(new ConfigureNamedOptions<TOptions>(name, configureOptions))
            .SingleInstance();
        builder.AddDataAnnotationValidatedOptions<TOptions>(name);
        return builder;
    }

    private static ContainerBuilder AddDataAnnotationValidatedOptions<TOptions>(this ContainerBuilder builder,
        string name) where TOptions : class
    {
        builder.RegisterInstance<IValidateOptions<TOptions>>(new DataAnnotationValidateOptions<TOptions>(name))
            .SingleInstance();
        return builder;
    }
}