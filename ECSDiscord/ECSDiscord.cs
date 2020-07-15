using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ECSDiscord
{
    class ECSDiscord
    {
        private const string
            BotCredentialFile = @"DiscordCredentials.json",
            LogFileName = "log.txt";
        private const RollingInterval
            LogInterval = RollingInterval.Day;


        private static DiscordBot _discordBot;

        public ECSDiscord()
        {
            startBot().Wait();
        }

        /// <summary>
        /// Starts Discord bot instance
        /// </summary>
        private Task startBot()
        {
            try
            {
                _discordBot = new DiscordBot(readBotToken());
                _discordBot.Start().ConfigureAwait(false);
                return Task.Delay(-1); // Run forever
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to start bot: \"{message}\"", ex.Message);
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Reads Discord bot token from the credential file.
        /// </summary>
        /// <returns>string containing the token.</returns>
        private static string readBotToken()
        {
            return JObject.Parse(File.ReadAllText(BotCredentialFile))["bot_token"].Value<string>();
        }

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(LogFileName, rollingInterval: LogInterval)
                .CreateLogger();
            new ECSDiscord();
        }
    }
}
