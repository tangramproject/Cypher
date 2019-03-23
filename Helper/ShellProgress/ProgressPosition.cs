using System;
using ShellProgressBar;

namespace TangramCypher.Helper.ShellProgress
{
    public class ProgressPosition: ProgressBarBase
    {
        readonly string message;
        readonly int totalTicks;
        readonly bool progressBarOnBottom;
        readonly char progressCharacter;

        public ProgressPosition(string message, int totalTicks, bool progressBarOnBottom, char progressCharacter)
        {
            this.message = message;
            this.totalTicks = totalTicks;
            this.progressBarOnBottom = progressBarOnBottom;
            this.progressCharacter = progressCharacter;
        }

        protected override void Start()
        {
            var options = new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Yellow,
                BackgroundColor = ConsoleColor.DarkGray,
                ProgressCharacter = progressCharacter,
                ProgressBarOnBottom = progressBarOnBottom,
            };
            using (var pbar = new ProgressBar(totalTicks, message, options))
            {
                TickToCompletion(pbar, totalTicks, sleep: 500);
            }
        }
    }
}
