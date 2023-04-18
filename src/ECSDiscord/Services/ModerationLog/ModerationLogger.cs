using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using ECSDiscord.Services.Bot;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECSDiscord.Services.ModerationLog;

public class ModerationLogger : IHostedService
{
    private string LogChannelName => "moderation-log";

    private readonly ILogger<ModerationLogger> _logger;
    private readonly DiscordBot _discordBot;

    public ModerationLogger(ILogger<ModerationLogger> logger, DiscordBot discordBot)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _discordBot = discordBot ?? throw new ArgumentNullException(nameof(discordBot));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Log(string title, string description, IUser user, IGuild guild)
    {
        _ = LogAsync(title, description, user, guild);
    }
    
    public Task LogAsync(string title, string description, IUser user, IGuild guild)
    {
        return LogAsync(new EmbedBuilder()
            .WithDescription($"**{title}**\n{description}".Trim())
            .WithColor(new Color(254, 204, 80))
            .WithCurrentTimestamp()
            .WithAuthor(x =>
            {
                x.Name = $"{user.Username}#{user.Discriminator}";
                x.IconUrl = user.GetAvatarUrl(ImageFormat.Auto, 256);
            })
            .WithFooter($"User: {user.Id}")
            .Build(), guild);
    }

    public void Log(Embed embed, IGuild guild)
    {
        _ = LogAsync(embed, guild);
    }
    
    public async Task LogAsync(Embed embed, IGuild guild)
    {
        var channel = await _discordBot.RequireLogChannelAsync(guild, LogChannelName);
        await channel.SendMessageAsync(embed: embed);
    }
}