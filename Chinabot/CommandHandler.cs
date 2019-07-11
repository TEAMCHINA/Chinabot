using System;
using System.Linq;
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
        private ILogger _logger;

        public CommandHandler(IServiceProvider services, DiscordSocketClient client, CommandService commands, ILogger logger)
        {
            _services = services;
            _client = client;
            _commands = commands;
            _logger = logger;

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

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _commands.CommandExecuted += handleCommandExecuted;
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
            var logChannel = await GetLogChannel(context);

            _logger.Log(LogSeverity.Info, $"Executing command: '{message}' on behalf of user {message.Author}", logChannel);

            // Execute the Command
            await _commands.ExecuteAsync(context, argPos, _services);
        }

        private async Task handleCommandExecuted(Optional<CommandInfo> commandArgs, ICommandContext context, IResult result)
        {
            var logger = _services.GetRequiredService<ILogger>();
            var logChannel = await GetLogChannel(context);

            // If the command succeeded just exit out; otherwise inform the user.
            if (result.IsSuccess && result.Error == null)
            {
                logger.Log(LogSeverity.Info, $"Command: '{context.Message.Content}' successful.", logChannel);
                return;
            }

            logger.Log(
                LogSeverity.Error,
                $"Command '{context.Message.Content}' resulted in error: {Environment.NewLine}\t{result.ErrorReason}",
                logChannel);

            if (result.Error != CommandError.UnknownCommand)
            {
                var embed = new EmbedBuilder();
                embed.Color = new Color(0x00FF0000);
                embed.Author = new EmbedAuthorBuilder();
                embed.Author.IconUrl = "https://cdn.discordapp.com/emojis/257437215615877129.png";
                embed.Author.Name = "Error executing command";
                embed.Description = result.ErrorReason;

                await context.Message.Channel.SendMessageAsync(string.Empty, false, embed.Build());
            }
        }

        private async Task<ITextChannel> GetLogChannel(ICommandContext context)
        {
            var textChannels = await context.Guild.GetTextChannelsAsync();

            var logChannel = textChannels
                .FirstOrDefault(c => c.Name == "bot_log");

            return logChannel;
        }
    }
}
