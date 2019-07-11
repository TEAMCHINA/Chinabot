using System;
using System.Threading.Tasks;
using System.Reflection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

using Chinabot.Logging;

namespace Chinabot
{
    public class CommandHandler
    {
        private CommandService _commands;
        private DiscordSocketClient _client;
        private IServiceProvider _services;

        public CommandHandler(IServiceProvider services, DiscordSocketClient client, CommandService commands)
        {
            _services = services;
            _client = client;
            _commands = commands;

            _client.MessageReceived += HandleCommand;
        }

        public async Task Install(IServiceProvider services)
        {
            // Create Command Service, inject it into Dependency Map
            _client = _services.GetRequiredService<DiscordSocketClient>();
            _commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async,
            });

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        }

        public async Task HandleCommand(SocketMessage parameterMessage)
        {
            var logger = _services.GetRequiredService<ILogger>();

            // Don't handle the command if it is a system message
            var message = parameterMessage as SocketUserMessage;
            if (message == null) return;

            // Mark where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message has a valid prefix, adjust argPos 
            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix('!', ref argPos))) return;

            logger.Log(LogSeverity.Info, $"Executing command: {message} on behalf of user {message.Author}");

            // Create a Command Context
            var context = new CommandContext(_client, message);
            // Execute the Command, store the result
            var result = await _commands.ExecuteAsync(context, argPos, _services);

            // If the command succeeded just exit out; otherwise inform the user.
            if (result.IsSuccess || !(result is ExecuteResult))
            {
                logger.Log(LogSeverity.Info, $"Command: {message} successful.");
                return;
            }

            var embed = new EmbedBuilder();
            embed.Color = new Color(0x00FF0000);
            embed.Author = new EmbedAuthorBuilder();
            embed.Author.IconUrl = "https://cdn.discordapp.com/emojis/257437215615877129.png";
            embed.Author.Name = "Error executing command";
            embed.Description = result.ErrorReason;

            logger.Log(LogSeverity.Error, $"Command {message.Content} resulted in error: {result.ErrorReason}");

            await message.Channel.SendMessageAsync(string.Empty, false, embed.Build());

        }
    }
}
