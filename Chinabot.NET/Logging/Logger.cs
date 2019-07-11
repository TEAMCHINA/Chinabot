using Discord;
using System;

namespace Chinabot.Logging
{
    public class Logger : ILogger
    {
        public void Log(LogMessage message)
        {
            Console.WriteLine("[{0:yyyy-MM-ddTHH:mm:ss}] [{1,-10}] {2}", DateTime.Now, message.Severity, message.Message);
        }

        public void Log(string message)
        {
            Log(new LogMessage(LogSeverity.Info, null, message));
        }

        public void Log(LogSeverity severity, string message)
        {
            Log(new LogMessage(severity, null, message));
        }
    }
}
