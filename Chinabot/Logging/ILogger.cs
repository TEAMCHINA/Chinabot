using Discord;

namespace Chinabot.Logging
{
    public interface ILogger
    {
        void Log(LogMessage message, ITextChannel logChannel = null);
        void Log(string message, ITextChannel logChannel = null);
        void Log(LogSeverity severity, string message, ITextChannel logChannel = null);
    }
}
