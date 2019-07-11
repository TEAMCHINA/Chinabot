using Discord;
using System;

namespace Chinabot.Logging
{
    public class Logger : ILogger
    {
        public void Log(LogMessage message, ITextChannel logChannel = null)
        {
            var logMessage = String.Format("[{0,-10}] {1}", message.Severity, message.Message);
            Console.WriteLine(string.Format("{0} {1}", DateTime.Now, logMessage));

            if (logChannel != null)
            {
                logChannel.SendMessageAsync(logMessage, true);
            }
        }

        public void Log(string message, ITextChannel logChannel = null)
        {
            Log(new LogMessage(LogSeverity.Info, null, message), logChannel);
        }

        public void Log(LogSeverity severity, string message, ITextChannel logChannel = null)
        {
            Log(new LogMessage(severity, null, message), logChannel);
        }
    }
}
