using Discord;
using System;

namespace Chinabot.Logging
{
    public class Logger : ILogger
    {
        public void Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
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
