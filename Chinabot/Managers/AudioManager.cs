using Chinabot.Logging;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Chinabot.Managers
{
    // Manages everything related to audio from joining voice channels to sending audio/TTS.
    public class AudioManager : IAudioManager
    {
        private readonly ConcurrentDictionary<ulong, AudioClientWrapper> ConnectedChannels = new ConcurrentDictionary<ulong, AudioClientWrapper>();
        private ILogger _logger;
        private IGuild _joinedGuild;
        private Random _randomNumberGenerator;

        // TODO: These need to be stored in a per guild lookup table.
        private int _eventCounter;
        private int _eventTriggeredCounter;

        private Dictionary<ulong, string> _introThemes;

        private const int InterruptionChance = 5;
        private const int InterruptionChanceMaxRange = 10000;

        public AudioManager(ILogger logger)
        {
            _logger = logger;
            _randomNumberGenerator = new Random();

            _introThemes = new Dictionary<ulong, string>();
            _introThemes.Add(147847182752415744, "Audio\\cena.mp3"); // Me
            _introThemes.Add(185598478401929216, "Audio\\omaewa.mp3"); // Tony
            _introThemes.Add(248756199355449345, "Audio\\price_is_right.mp3"); // Michael
            _introThemes.Add(156946950564872192, "Audio\\nina.mp3"); // Nina?!?
            _introThemes.Add(71662440248508416, "Audio\\tonightyou.mp3"); // Dave
            _introThemes.Add(65661105761943552, "Audio\\ark.mp3"); // AJ

            ResetCounters();
        }

        private void ResetCounters()
        {
            _logger.Log(LogSeverity.Info, "Interrupt counters reset");
            _eventCounter = 0;
            _eventTriggeredCounter = 0;
        }

        public async Task JoinDefaultAudioChannel(IGuild guild)
        {
            var logChannel = await GetLogChannel(guild);
            var channels = await guild.GetVoiceChannelsAsync();

            var target = channels
                .OrderBy(c => c.Position)
                .First();

            _logger.Log(new LogMessage(
                LogSeverity.Info,
                this.ToString(),
                $"No target channel provided, attempting to join channel: {target.Name}"),
                logChannel);
            var client = await ((IVoiceChannel)target).ConnectAsync();

            var wrapper = new AudioClientWrapper(target.Id, client);

            ConnectedChannels.AddOrUpdate(guild.Id, wrapper, (key, oldWrapper) => wrapper);
        }

        public async Task JoinAudioChannel(IGuild guild, IVoiceChannel target)
        {
            var logChannel = await GetLogChannel(guild);
            if (target.Guild.Id != guild.Id)
            {
                _logger.Log(LogSeverity.Warning, "Attempted to join a channel from a different guild.", logChannel);
                return;
            }

            _logger.Log(new LogMessage(
                LogSeverity.Info,
                this.ToString(),
                $"Attempting to join channel: {target.Name}"), logChannel);
            var client = await target.ConnectAsync();

            _joinedGuild = guild;

            var wrapper = new AudioClientWrapper(target.Id, client);

            client.SpeakingUpdated += Client_SpeakingUpdated;
            ConnectedChannels.AddOrUpdate(guild.Id, wrapper, (key, oldWrapper) => wrapper);
        }

        private async Task Client_SpeakingUpdated(ulong userId, bool isSpeaking)
        {
            var user = await _joinedGuild.GetUserAsync(userId);
            var logChannel = await GetLogChannel(_joinedGuild);

            if (user.IsBot)
            {
                return;
            }

            if (_eventCounter == int.MaxValue)
            {
                ResetCounters();
            }

            _eventCounter++;

            bool interrupt = _randomNumberGenerator.Next(0, InterruptionChanceMaxRange) <= InterruptionChance;
            if (isSpeaking && interrupt)
            {
                _eventTriggeredCounter++;
                _logger.Log(
                    LogSeverity.Info,
                    $"Interruption triggered. {_eventTriggeredCounter} interrupts in {_eventCounter} opportunities.",
                    logChannel);

                bool knowYourRole = _randomNumberGenerator.Next() % 2 == 1;
                var audioSource = knowYourRole
                    ? "Audio\\know_your_role.mp3"
                    : "Audio\\it_doesnt_matter.mp3";
                // HACK: _joinedGuild will only work if the bot is connected to a single server at a time.
                await SendAudioAsync(_joinedGuild, audioSource);
            }
        }

        public async Task LeaveAudioChannels(IGuild guild)
        {
            var logChannel = await GetLogChannel(guild);
            AudioClientWrapper wrapper;

            if (ConnectedChannels.TryRemove(guild.Id, out wrapper))
            {
                await wrapper.Client.StopAsync();
                _logger.Log(LogSeverity.Info, $"Disconnected from voice on {guild.Name}.", logChannel);
            }
        }

        public async Task SendAudioAsync(IGuild guild, string path)
        {
            // TODO: Add a check to prevent audio from being played concurrently (to the same guild?).
            var logChannel = await GetLogChannel(guild);

            if (!File.Exists(path))
            {
                _logger.Log(LogSeverity.Error, $"No file found at {path}", logChannel);
                throw new FileNotFoundException($"No file found at {path}");
            }

            AudioClientWrapper wrapper = null;
            if (ConnectedChannels.TryGetValue(guild.Id, out wrapper))
            {
                _logger.Log(LogSeverity.Info, $"Playing audio file: '{path}'.", logChannel);
                using (var ffmpeg = CreateProcess(path))
                using (var stream = wrapper.Client.CreatePCMStream(AudioApplication.Music))
                {
                    try
                    {
                        await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream);
                    }
                    finally
                    {
                        await stream.FlushAsync();
                    }
                }
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
            // TODO: TTS the input.
        }

        public async Task UserVoiceStateUpdatedHandler(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            var gUser = user as IGuildUser;
            var nickname = gUser.Nickname;
            nickname = string.IsNullOrWhiteSpace(nickname) ? user.Username : nickname;
            var guild = gUser.Guild;
            var logChannel = await GetLogChannel(guild);

            if (user.IsBot)
            {
                if (oldState.VoiceChannel != null)
                {
                    AudioClientWrapper wrapper;
                    ConnectedChannels.TryGetValue(guild.Id, out wrapper);

                    wrapper.ChannelId = newState.VoiceChannel.Id;
                    ConnectedChannels.AddOrUpdate(guild.Id, wrapper, (key, newWrapper) => wrapper);
                }

                return;
            }

            // User wasn't in voice and still isn't, this case should never be hit.
            if (oldState.VoiceChannel == null && newState.VoiceChannel == null)
            {
                _logger.Log($"User: {nickname} is not in voice.", logChannel);
            }
            // User was not in voice previously.
            else if (oldState.VoiceChannel == null)
            {
                _logger.Log($"User: {nickname} joined {newState.VoiceChannel.Name}", logChannel);
                var wrapper = ConnectedChannels[guild.Id];

                if (wrapper.ChannelId == newState.VoiceChannel.Id)
                {
                    if (_introThemes.ContainsKey(gUser.Id))
                    {
                        await SendAudioAsync(guild, _introThemes[gUser.Id]);
                    } else
                    {
                        await Speak(guild, $"{nickname} joined {newState.VoiceChannel.Name}.");
                    }
                }
            }
            // User is no longer in a voice channel.
            else if (newState.VoiceChannel == null)
            {
                _logger.Log($"User: {nickname} left voice chat.", logChannel);
                await Speak(guild, $"{nickname} has left voice chat.");
            }
            // User changed channels.
            else if (oldState.VoiceChannel.Id != newState.VoiceChannel.Id)
            {
                _logger.Log($"User: {nickname} moved to {newState.VoiceChannel.Name} (from {oldState.VoiceChannel.Name})", logChannel);
                await Speak(guild, $"{nickname} moved to {newState.VoiceChannel.Name}.");
            }
        }

        private Process CreateProcess(string path)
        {
            var ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1 -filter:a loudnorm",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            return Process.Start(ffmpeg);
        }

        private async Task<ITextChannel> GetLogChannel(IGuild guild)
        {
            var channels = await guild.GetTextChannelsAsync();
            var logChannel = channels
                .FirstOrDefault(c => c.Name == "bot_log");

            return logChannel;
        }
    }
}
