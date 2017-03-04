using Discord;

namespace Chinabot.Logging
{
    public interface ILogger
    {
        void Log(LogMessage message);
        void Log(string message);
        void Log(LogSeverity severity, string message);
    }
}
