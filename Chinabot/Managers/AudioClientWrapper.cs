using Discord.Audio;

namespace Chinabot.Managers
{
    // This class is only necessary because the AudioClient class is marked as internal
    // (https://github.com/discord-net/Discord.Net/blob/dev/src/Discord.Net.WebSocket/Audio/AudioClient.cs#L19)
    public class AudioClientWrapper
    {
        public ulong ChannelId { get; set; }
        public IAudioClient Client { get; set; }

        public AudioClientWrapper(ulong channelId, IAudioClient client)
        {
            ChannelId = channelId;
            Client = client;
        }
    }
}
