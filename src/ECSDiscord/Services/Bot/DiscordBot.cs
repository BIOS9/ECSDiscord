using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace ECSDiscord.Services.Bot;

public class DiscordBot : IHostedService
{
    private readonly ILogger<DiscordSocketClient> _botLogger;
    private readonly ILogger<DiscordBot> _logger;

    private readonly IReadOnlyDictionary<LogSeverity, LogLevel>
        _logLevelMap = // Maps Discord.NET logging levels to Microsoft extensions logging levels.
            new Dictionary<LogSeverity, LogLevel>
            {
                { LogSeverity.Debug, LogLevel.Trace },
                { LogSeverity.Verbose, LogLevel.Debug },
                { LogSeverity.Info, LogLevel.Information },
                { LogSeverity.Warning, LogLevel.Warning },
                { LogSeverity.Error, LogLevel.Error },
                { LogSeverity.Critical, LogLevel.Critical }
            };

    private readonly DiscordBotOptions _options;

    public DiscordBot(IOptions<DiscordBotOptions> options, ILoggerFactory loggerFactory)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<DiscordBot>();
        _botLogger = loggerFactory.CreateLogger<DiscordSocketClient>();

        DiscordClient = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Verbose,
            LogGatewayIntentWarnings = true,
            AlwaysDownloadUsers = true,
            MessageCacheSize = 1000,
            GatewayIntents = GatewayIntents.Guilds
                             | GatewayIntents.MessageContent
                             | GatewayIntents.DirectMessages
                             | GatewayIntents.GuildMembers
                             | GatewayIntents.GuildMessages
                             | GatewayIntents.GuildMessageReactions
        });
    }

    public DiscordSocketClient DiscordClient { get; }
    public ulong GuildId => _options.GuildId;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Discord bot");
        DiscordClient.Log += DiscordClient_Log;
        DiscordClient.GuildAvailable += _discord_GuildAvailable;
        await DiscordClient.SetActivityAsync(new Game(_options.StatusText));
        await DiscordClient.LoginAsync(TokenType.Bot, _options.Token);
        await DiscordClient.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Discord bot");
        await DiscordClient.StopAsync();
        DiscordClient.GuildAvailable -= _discord_GuildAvailable;
        DiscordClient.Log += DiscordClient_Log;
    }

    private Task DiscordClient_Log(LogMessage arg)
    {
        var level = _logLevelMap[arg.Severity];
        _botLogger.Log(level, arg.Exception, "{Source} {Message}", arg.Source, arg.Message);
        return Task.CompletedTask;
    }

    private async Task _discord_GuildAvailable(SocketGuild arg)
    {
        if (arg.Id != _options.GuildId)
        {
            Log.Warning("Leaving guild {Guild} Config guildId does not match", arg.Name);
            await arg.LeaveAsync();
        }
    }
    
    /// <summary>
    /// Returns log channel with specified name in a guild.
    /// If the channel does not exist, it will be created.
    /// </summary>
    public async Task<IMessageChannel> RequireLogChannelAsync(IGuild guild, string name)
    {
        var channels = await guild.GetTextChannelsAsync();
        IMessageChannel? channel = channels.FirstOrDefault(x => x.Name.Equals(name));
        if (channel == null)
        {
            _logger.LogInformation("Creating channel {Channel} in guild {GuildName} {GuildID}", 
                name, guild.Name, guild.Id);
            var permissionOverrides = new OverwritePermissions(viewChannel: PermValue.Deny);
            var newChannel = await guild.CreateTextChannelAsync(name);
            await newChannel.AddPermissionOverwriteAsync(guild.EveryoneRole, permissionOverrides);
            return newChannel;
        }
        return channel;
    }
}