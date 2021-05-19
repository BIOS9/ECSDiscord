using ECSDiscord.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ECSWebDashboard.Util
{
    public static class DiscordServiceProvider
    {
        internal static IServiceProvider _serviceProvider;

        public static IServiceCollection AddDiscord(this IServiceCollection collection)
        {
            ECSDiscord.ECSDiscord discord = new ECSDiscord.ECSDiscord();
            return discord.ConfigureServices(collection);
        }

        public static void UseDiscord(this IApplicationBuilder builder, LoggerConfiguration loggerConfiguration)
        {
            Log.Logger = loggerConfiguration.CreateLogger();
            ECSDiscord.ECSDiscord discord = builder.ApplicationServices.GetRequiredService<ECSDiscord.ECSDiscord>();
            _serviceProvider = builder.ApplicationServices;
            Task.Run(() => discord.StartAsync(builder.ApplicationServices));
        }

        public static bool IsMember(AuthorizationHandlerContext context)
        {
            var service = _serviceProvider.GetRequiredService<AdministrationService>();
            return service.IsMember(ulong.Parse(context.User.Claims.First(x => x.Type == "discord:id").Value));
        }

        public static bool IsAdmin(AuthorizationHandlerContext context)
        {
            var service = _serviceProvider.GetRequiredService<AdministrationService>();
            Claim idClaim = context.User.Claims.FirstOrDefault(x => x.Type == "discord:id");
            if (idClaim == null)
                return false;
            return service.IsAdmin(ulong.Parse(idClaim.Value));
        }
    }
}