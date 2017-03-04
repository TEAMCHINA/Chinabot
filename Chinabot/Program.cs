using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Chinabot.Logging;
using Discord.Audio;
using Chinabot.Managers;

namespace Chinabot
{
    public class Program
    {
        // Convert our sync main to an async main.
        public static void Main(string[] args) =>
            new Program().Start().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandHandler _handler;

        public async Task Start()
        {
            // Define the DiscordSocketClient
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                AudioMode = AudioMode.Outgoing,
            });

            var token = AppResources.BotToken;
            var logger = new Logger();

            // Login and connect to Discord.
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            var map = new DependencyMap();
            map.Add(_client);
            map.Add<ILogger>(logger);
            map.Add<IAudioManager>(new AudioManager(logger));

            _handler = new CommandHandler();
            await _handler.Install(map);

            _client.Log += (msg) =>
            {
                logger.Log(msg);
                return Task.CompletedTask;
            };

            _client.UserVoiceStateUpdated += (user, oldState, newState) =>
                {
                    var nickname = (user as IGuildUser).Nickname;

                    // User wasn't in voice and still isn't, this case should never be hit.
                    if (oldState.VoiceChannel == null && newState.VoiceChannel == null)
                    {
                        logger.Log($"User: {nickname} is not in voice.");
                    }
                    // User was not in voice previously.
                    else if (oldState.VoiceChannel == null)
                    {
                        logger.Log($"User: {nickname} joined {newState.VoiceChannel.Name}");
                    }
                    // User is no longer in a voice channel.
                    else if (newState.VoiceChannel == null)
                    {
                        logger.Log($"User: {nickname} left voice chat.");
                    // User changed channels.
                    } else if (oldState.VoiceChannel.Id != newState.VoiceChannel.Id)
                    {
                        logger.Log($"User: {nickname} moved to {newState.VoiceChannel.Name} (from {oldState.VoiceChannel.Name})");
                    }

                    return Task.CompletedTask;
                };

            // Block this program until it is closed.
            await Task.Delay(-1);
        }
    }
}