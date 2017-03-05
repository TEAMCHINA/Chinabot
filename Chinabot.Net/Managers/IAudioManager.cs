using Discord;
using System.Threading.Tasks;

namespace Chinabot.Net.Managers
{
    public interface IAudioManager
    {
        Task JoinAudioChannel(IGuild guild, IVoiceChannel target);
        Task LeaveAudioChannels(IGuild guild);
        Task SendAudioAsync(IGuild guild, string path);
        Task Speak(IGuild guild, string input);
    }
}
