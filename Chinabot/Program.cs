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

            var token = "Mjg1NjA0NDYzMzEyNTAyNzg0.C5vdKw.F8BpZc8y4TuV-LnOP30Cf0qWU4c";
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

            // Block this program until it is closed.
            await Task.Delay(-1);
        }
    }
}