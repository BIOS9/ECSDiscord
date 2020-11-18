using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Translation.Plugin
{
    public class AlreadyInitializedException : Exception
    {
        public AlreadyInitializedException(string message) : base(message) { }
    }
}
