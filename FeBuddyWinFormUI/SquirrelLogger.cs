using FeBuddyLibrary.Helpers;
using Squirrel.SimpleSplat;
using System;
using System.ComponentModel;

namespace FeBuddyWinFormUI
{
    class SquirrelLogger : ILogger
    {
        public LogLevel Level { get; set; }

        public void Write([Localizable(false)] string message, LogLevel logLevel)
        {
            if (logLevel >= Level)
                Logger.LogMessage(logLevel.ToString().ToUpper(), message);
        }

        public static void Register()
        {
            var sqLog = new SquirrelLogger();
            SquirrelLocator.CurrentMutable.Register(() => sqLog, typeof(ILogger));
        }
    }
}
