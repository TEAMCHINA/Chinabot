using Discord;
using Discord.Audio;
using System.Threading.Tasks;

namespace Chinabot.Managers
{
    public interface IAudioManager
    {
        Task JoinAudioChannel(IGuild guild, IVoiceChannel target);
        Task SendAudioAsync(IGuild guild, string path);
    }
}
