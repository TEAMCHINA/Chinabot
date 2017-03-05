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
                AudioMode = AudioMode.Outgoing,
            });

            var token = AppResources.BotToken;
            _logger = new Logger();
            _audioManager = new AudioManager(_logger);

            // Login and connect to Discord.
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            var map = new DependencyMap();
            map.Add(_client);
            map.Add(_logger);
            map.Add(_audioManager);

            _handler = new CommandHandler();
            await _handler.Install(map);

            _client.Log += (msg) =>
            {
                _logger.Log(msg);
                return Task.CompletedTask;
            };

            _client.UserVoiceStateUpdated += async (user, oldState, newState) =>
            {
                var nickname = (user as IGuildUser).Nickname;
                nickname = string.IsNullOrWhiteSpace(nickname) ? user.Username : nickname;

                var guild = (user as IGuildUser).Guild;

                // User wasn't in voice and still isn't, this case should never be hit.
                if (oldState.VoiceChannel == null && newState.VoiceChannel == null)
                {
                    _logger.Log($"User: {nickname} is not in voice.");
                }
                // User was not in voice previously.
                else if (oldState.VoiceChannel == null)
                {
                    _logger.Log($"User: {nickname} joined {newState.VoiceChannel.Name}");
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
                    await _audioManager.Speak(guild, $"{nickname} joined {newState.VoiceChannel.Name}.");
                }
            };

            // Block this program until it is closed.
            await Task.Delay(-1);
        }
    }
}