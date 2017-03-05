using System.Threading.Tasks;
using System.Reflection;
using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Chinabot.Net.Logging;

namespace Chinabot.Net
{
    public class CommandHandler
    {
        private CommandService _commands;
        private DiscordSocketClient _client;
        private IDependencyMap _map;

        public async Task Install(IDependencyMap map)
        {
            // Create Command Service, inject it into Dependency Map
            _client = map.Get<DiscordSocketClient>();
            _commands = new CommandService();
            map.Add(_commands);
            _map = map;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());

            _client.MessageReceived += HandleCommand;
        }

        public async Task HandleCommand(SocketMessage parameterMessage)
        {
            // Don't handle the command if it is a system message
            var message = parameterMessage as SocketUserMessage;
            if (message == null) return;

            // Mark where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message has a valid prefix, adjust argPos 
            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix('!', ref argPos))) return;

            // Create a Command Context
            var context = new CommandContext(_client, message);
            // Execute the Command, store the result
            var result = await _commands.ExecuteAsync(context, argPos, _map);

            // If the command failed, notify the user
            if (result.IsSuccess || !(result is ExecuteResult))
            {
                return;
            }

            var embed = new EmbedBuilder();
            embed.Color = new Color(0x00FF0000);
            embed.Author = new EmbedAuthorBuilder();
            embed.Author.IconUrl = "https://cdn.discordapp.com/emojis/257437215615877129.png";
            embed.Author.Name = "Error executing command";
            embed.Description = result.ErrorReason;

            var logger = _map.Get<ILogger>();
            logger.Log(LogSeverity.Error, $"Command {message.Content} resulted in error: {result.ErrorReason}");

            await message.Channel.SendMessageAsync(string.Empty, false, embed);
        }
    }
}
