using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Chinabot.Logging;
using Chinabot.Managers;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

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
        private IChannelManager _channelManager;

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
            _channelManager = new ChannelManager(_logger, _client);

            var serviceCollection = ConfigureServices();

            var clientReady = false;

            _client.Ready += async () => {
                Task.Run(async () => {

                    await _channelManager.Start();
                });
            };

            _handler = serviceCollection.GetRequiredService<CommandHandler>();
            await _handler.Install(serviceCollection);

            _client.Log += (msg) =>
            {
                _logger.Log(msg);
                return Task.CompletedTask;
            };

            _client.UserVoiceStateUpdated += _audioManager.UserVoiceStateUpdatedHandler;

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
                .AddSingleton(_channelManager)

                .BuildServiceProvider();
        }
    }
}
