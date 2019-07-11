using Discord;
using System.Threading.Tasks;

namespace Chinabot.Managers
{
    public interface IAudioManager
    {
        Task JoinDefaultAudioChannel(IGuild guild);
        Task JoinAudioChannel(IGuild guild, IVoiceChannel target);
        Task LeaveAudioChannels(IGuild guild);
        Task SendAudioAsync(IGuild guild, string path);
        Task Speak(IGuild guild, string input);
    }
}
