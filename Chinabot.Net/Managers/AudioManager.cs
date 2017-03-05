using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Speech.Synthesis;
using System.Threading.Tasks;

using Chinabot.Net.Logging;
using Discord;
using Discord.Audio;
using NAudio.Wave;

namespace Chinabot.Net.Managers
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
                $"Attempting to join channel: {target.Name}"));
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

        public async Task Speak(IGuild guild, string input)
        {
            IAudioClient client = null;
            if (ConnectedChannels.TryGetValue(guild.Id, out client))
            {
                _logger.Log(LogSeverity.Info, $"Speaking, TTS text: {input}");

                using (var ms = new MemoryStream())
                {
                    using (var synth = new SpeechSynthesizer())
                    {
                        synth.SetOutputToWaveStream(ms);
                        synth.Speak(input);

                        await ms.FlushAsync();
                        ms.Seek(0, SeekOrigin.Begin);
                    }

                    var outputFormat = new WaveFormat(48000, 16, 2);
                    var stream = client.CreatePCMStream(AudioApplication.Mixed, 1920);

                    using (var wav = new WaveFileReader(ms))
                    using (var pcm = new MediaFoundationResampler(wav, outputFormat))
                    {
                        pcm.ResamplerQuality = 50;
                        var bs = outputFormat.AverageBytesPerSecond / 50;
                        var buff = new byte[bs];
                        int bc = 0;

                        while ((bc = pcm.Read(buff, 0, bs)) > 0)
                        {
                            if (bc < bs)
                            {
                                for (var i = bc; i < bs; i++)
                                {
                                    buff[i] = 0;
                                }

                                stream.Write(buff, 0, bs);
                            }
                        }
                    }
                }
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
