using System;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Tangram.Bamboo.Helper
{
    public static class Logger
    {
        public static void LogException(IConsole console, ILogger logger, Exception e)
        {
            console.BackgroundColor = ConsoleColor.Red;
            console.ForegroundColor = ConsoleColor.White;
            console.WriteLine(e.ToString());
            logger.LogError(e, Environment.NewLine);
            console.ResetColor();
        }

        public static void LogWarning(IConsole console, ILogger logger, string message)
        {
            console.ForegroundColor = ConsoleColor.Yellow;
            console.WriteLine(message);
            console.ResetColor();
            logger.LogWarning(message);
        }
    }
}
