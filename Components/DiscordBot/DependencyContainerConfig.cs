using Autofac;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBot
{
    internal class DependencyContainerConfig
    {
        public static ContainerBuilder CreateBuilder()
        {
            var builder = new ContainerBuilder();

            builder.Populate(new ServiceCollection());

            builder.RegisterInstance(new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,       // Tell the logger to give all info, log level will be handled by the BotLogger.
                MessageCacheSize = 1000,            // Cache 1,000 messages per channel,
                DefaultRetryMode = RetryMode.AlwaysRetry, // Retry failed API calls.
            }));

            builder.RegisterInstance(new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Debug,       // Tell the logger to give all info, log level will be handled by the CommandLogger.
                DefaultRunMode = RunMode.Async,     // Force all commands to run async by default.
                CaseSensitiveCommands = false,
                IgnoreExtraArgs = true
            }));

            builder.RegisterType<CommandHandler>();

            return builder;
        }
    }
}
