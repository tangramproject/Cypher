using System;
using ShellProgressBar;

namespace TangramCypher.Helper.ShellProgress
{
    public class Styling: ProgressBarBase
    {
        readonly string message;
        readonly int totalTicks;

        public Styling(string message, int totalTicks)
        {
            this.message = message;
            this.totalTicks = totalTicks;
        }

        protected override void Start()
        {
            var options = new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Yellow,
                ForegroundColorDone = ConsoleColor.DarkGreen,
                BackgroundColor = ConsoleColor.DarkGray,
                BackgroundCharacter = '\u2593'
            };
            using (var pbar = new ProgressBar(totalTicks, message, options))
            {
                TickToCompletion(pbar, totalTicks, sleep: 500);
            }
        }
    }
}
