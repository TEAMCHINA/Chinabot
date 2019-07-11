using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Chinabot.Logging;
using Chinabot.Managers;
using Microsoft.Extensions.DependencyInjection;
using Chinabot.NET;

namespace Chinabot
{
    public class Program
    {
        // Convert our sync main to an async main.
        public static void Main(string[] args) =>
            new Program()
                .Start()
                .GetAwaiter()
                .GetResult();

        private DiscordSocketClient _client;
        private CommandHandler _handler;

        /* TODO: I don't like storing these dependencies as local fields but
         * they're useful for now since the events fire against the
         * DiscordSocketClient; ideally the only thing that should be happening
         * in the main loop would be setting up the client and connecting, but
         * I'll clean this up later.
         */
        private ILogger _logger;
        private IAudioManager _audioManager;

        public async Task Start()
        {
            // Define the DiscordSocketClient
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                //AudioMode = AudioMode.Outgoing,
            });

            var token = AppResources.BotToken;
            _logger = new Logger();
            _audioManager = new AudioManager(_logger);

            var serviceCollection = ConfigureServices();

            _handler = serviceCollection.GetRequiredService<CommandHandler>();
            await _handler.Install(serviceCollection);

            _client.Log += (msg) =>
            {
                _logger.Log(msg);
                return Task.CompletedTask;
            };

            _client.UserVoiceStateUpdated += async (user, oldState, newState) =>
            {
                if (user.IsBot) return;

                var gUser = user as IGuildUser;
                var nickname = gUser.Nickname;
                nickname = string.IsNullOrWhiteSpace(nickname) ? user.Username : nickname;

                var guild = gUser.Guild;

                // User wasn't in voice and still isn't, this case should never be hit.
                if (oldState.VoiceChannel == null && newState.VoiceChannel == null)
                {
                    _logger.Log($"User: {nickname} is not in voice.");
                }
                // User was not in voice previously.
                else if (oldState.VoiceChannel == null)
                {
                    _logger.Log($"User: {nickname} joined {newState.VoiceChannel.Name}");

                    if (gUser.Username == "TEAMCHINA")
                    {
                        await _audioManager.SendAudioAsync(guild, "Audio\\cena.mp3");
                    }

                    await _audioManager.Speak(guild, $"{nickname} joined {newState.VoiceChannel.Name}.");

                }
                // User is no longer in a voice channel.
                else if (newState.VoiceChannel == null)
                {
                    _logger.Log($"User: {nickname} left voice chat.");
                    await _audioManager.Speak(guild, $"{nickname} has left voice chat.");
                }
                // User changed channels.
                else if (oldState.VoiceChannel.Id != newState.VoiceChannel.Id)
                {
                    _logger.Log($"User: {nickname} moved to {newState.VoiceChannel.Name} (from {oldState.VoiceChannel.Name})");
                    await _audioManager.Speak(guild, $"{nickname} moved to {newState.VoiceChannel.Name}.");
                }
            };


            // Login and connect to Discord.
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this program until it is closed.
            await Task.Delay(-1);
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                // Base
                .AddSingleton(_client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandler>()
                // Logging
                .AddSingleton(_logger)
                // Add additional services here...
                .AddSingleton(_audioManager)

                .BuildServiceProvider();
        }
    }
}