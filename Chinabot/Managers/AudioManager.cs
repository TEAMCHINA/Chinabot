using Chinabot.Logging;
using Discord;
using Discord.Audio;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Chinabot.Managers
{
    public class AudioManager : IAudioManager
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();
        private ILogger _logger;

        public AudioManager(ILogger logger)
        {
            _logger = logger;
        }

        public async Task JoinAudioChannel(IGuild guild, IVoiceChannel target)
        {
            if (target.Guild.Id != guild.Id)
            {
                _logger.Log(LogSeverity.Warning, "Attempted to join a channel from a different guild.");
                return;
            }

            _logger.Log(new LogMessage(
                LogSeverity.Info,
                this.ToString(),
                $"Attempting to join  channel: {target.Name}"));
            var client = await target.ConnectAsync();

            ConnectedChannels.AddOrUpdate(guild.Id, client, (key, oldClient) => client);
        }

        public async Task LeaveAudioChannels(IGuild guild)
        {
            IAudioClient client;

            if (ConnectedChannels.TryRemove(guild.Id, out client))
            {
                await client.StopAsync();
                _logger.Log(LogSeverity.Info, $"Disconnected from voice on {guild.Name}.");
            }
        }

        public async Task SendAudioAsync(IGuild guild, string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"No file found at {path}");
            }

            IAudioClient client = null;
            if (ConnectedChannels.TryGetValue(guild.Id, out client))
            {
                _logger.Log(LogSeverity.Info, $"Playing audio file: '{path}'.");
                var output = CreateStream(path).StandardOutput.BaseStream;
                var stream = client.CreatePCMStream(AudioApplication.Music, 1920);
                await output.CopyToAsync(stream);
                await stream.FlushAsync().ConfigureAwait(false);
            }
        }

        private Process CreateStream(string path)
        {
            var ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i {path} -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            return Process.Start(ffmpeg);
        }
    }
}
