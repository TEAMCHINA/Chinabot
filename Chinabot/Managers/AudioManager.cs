using Chinabot.Logging;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Chinabot.Managers
{
    // Manages everything related to audio from joining voice channels to sending audio/TTS.
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

        // Unfortunately .NET Core does not have native support for TTS yet as
        // the .NET Framework TTS functionality is heavily dependent on Windows
        // APIs. While there are some 3rd party "hacks" available, those are
        // also dependent on Windows machines which sort of defeats the point
        // of developing in .NET Core so, for now, we're leveraging Discords TTS
        // message functionality, rather than having the bot create the audio.
        // Because the entirety of this functoinality is encapsulated here we can
        // revisit this in the future to add native support.
        public async Task Speak(IGuild guild, string input)
        {
            var socketGuild = guild as SocketGuild;
            var vlChannel = socketGuild.Channels
                .OfType<ITextChannel>()
                .FirstOrDefault(c => c.Name == "voice_log");

            await vlChannel.SendMessageAsync(input, true);
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
