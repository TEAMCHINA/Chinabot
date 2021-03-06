﻿using Chinabot.Logging;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chinabot.Managers
{
    public class ChannelManager : IChannelManager
    {
        private const int MAX_CHANNEL_NAME_LENGTH = 20;
        private const string ACTIVE_CATEGORY_NAME = "Active Conversations";
        private const string INACTIVE_CATEGORY_NAME = "Conversation Graveyard";

        private const int CLEANUP_TIME_DELAY_IN_SECONDS = 300;

        private ILogger _logger;
        private DiscordSocketClient _client;

        private Timer _timer;

        public ChannelManager(ILogger logger, DiscordSocketClient client)
        {
            _logger = logger;
            _client = client;

            _client.MessageReceived += _client_MessageReceived;
        }

        public async Task Start()
        {
            await SetCleanupLoop();
        }

        private async Task _client_MessageReceived(SocketMessage arg)
        {
            var channel = arg.Channel as ITextChannel;
            var guild = (arg.Channel as SocketGuildChannel).Guild as IGuild;

            // Get our categories
            var categories = await GetConversationCategories(guild);

            if (channel.CategoryId == categories.InactiveConversations.Id)
            {
                var logChannel = await GetLogChannel(guild);
                await channel.ModifyAsync(p => p.CategoryId = categories.ActiveConversations.Id);
                _logger.Log(LogSeverity.Info, $"{arg.Author.Username} brings {channel.Mention} back from the dead!", logChannel);
            }
        }

        public async Task CreateConversationChannel(ICommandContext context, string input)
        {
            var guild = context.Guild;
            var channelName = input.Split(' ')[0];
            var remainder = input.Length > channelName.Length
                ? $"<@{context.Message.Author.Id}>: {input.Substring(channelName.Length + 1)}"
                : $"Let's talk about {channelName} baby. Let's talk about you and me.";

            ValidateChannelName(channelName);

            var categories = await GetConversationCategories(guild);

            // Check if the channel already exists.
            var channels = await guild.GetTextChannelsAsync();
            var channel = channels.FirstOrDefault(c => string.Compare(channelName, c.Name, true) == 0);

            if (channel != null)
            {
                // Channel exists, need to see what Category it belongs to.
                if (channel.CategoryId == categories.ActiveConversations.Id)
                {
                    // Channel already exists and is in the active category.
                    await SendConversationNotice(context.Message.Channel, $"{context.Message.Author.Username} is talking in {channel.Mention}");
                }
                else if (channel.CategoryId == categories.InactiveConversations.Id)
                {
                    // Channel exists but was in the graveyard. c 'revive' channel
                    await channel.ModifyAsync(p => p.CategoryId = categories.InactiveConversations.Id);

                    await SendConversationNotice(context.Message.Channel, $"{context.Message.Author.Username} casts 'revive thread' on {channel.Mention}");
                }

            }
            else
            {
                channel = await guild.CreateTextChannelAsync(channelName, p => p.CategoryId = categories.ActiveConversations.Id);
                await SendConversationNotice(context.Message.Channel, $"{context.Message.Author.Username} starts a conversation about {channel.Mention}");
            }

            await channel.SendMessageAsync(remainder);
            await context.Message.DeleteAsync();
        }

        public async Task PromoteChannel(ICommandContext context, string input)
        {
            var guild = context.Guild;
            var channelName = input.Trim();

            ValidateChannelName(channelName);

            // Check if the channel already exists.
            var channels = await guild.GetTextChannelsAsync();
            var channel = channels.FirstOrDefault(c => string.Compare(channelName, c.Name, true) == 0);

            if (channel == null)
            {
                throw new ArgumentException($"No channel \"{channelName}\" found.");
            }

            var categories = await GetConversationCategories(guild);

            if (channel.CategoryId == null)
            {
                // Channel is already a real boy!
                await SendConversationNotice(context.Message.Channel, $"Channel {channel.Mention} is already a real boy!");
            }
            else if (channel.CategoryId == categories.ActiveConversations.Id) {
                // Promote it to a full channel! (ie. no category)
                await channel.ModifyAsync(p => p.CategoryId = null);
                await SendConversationNotice(context.Message.Channel, $"{context.Message.Author.Username} turns {channel.Mention} into a real boy.");
            } else if (channel.CategoryId == categories.InactiveConversations.Id)
            {
                await channel.ModifyAsync(p => p.CategoryId = categories.ActiveConversations.Id);
                await SendConversationNotice(context.Message.Channel, $"{context.Message.Author.Username} casts 'revive thread' on {channel.Mention}");
            }
        }

        public async Task DemoteChannel(ICommandContext context, string input)
        {
            var guild = context.Guild;
            var channelName = input.Trim();

            ValidateChannelName(channelName);

            // Check if the channel already exists.
            var channels = await guild.GetTextChannelsAsync();
            var channel = channels.FirstOrDefault(c => string.Compare(channelName, c.Name, true) == 0);

            if (channel == null)
            {
                throw new ArgumentException($"No channel \"{channelName}\" found.");
            }

            var categories = await GetConversationCategories(guild);

            if (channel.CategoryId != categories.ActiveConversations.Id)
            {
                throw new ArgumentException($"Channel \"{channelName}\" is not eligible to be demoted.");
            }

            await channel.ModifyAsync(p => p.CategoryId = categories.InactiveConversations.Id);
            await SendConversationNotice(context.Message.Channel, $"{context.Message.Author.Username} tries to get the last word in on {channel.Mention}");
        }

        private async Task SetCleanupLoop()
        {
            // This will run on a background thread. It's dirty, but async/await with Timers is dirtier.
            while (true)
            {
                await CleanupChannels();

                await Task.Delay(CLEANUP_TIME_DELAY_IN_SECONDS * 1000);
            }
        }

        private async Task CleanupChannels()
        {
            var guilds = _client.Guilds;

            foreach (var g in guilds)
            {
                IGuild guild = g as IGuild;
                var logChannel = await GetLogChannel(g);

                var categories = await GetConversationCategories(guild);

                var channels = (await guild.GetTextChannelsAsync())
                    .Where(c => c.CategoryId == categories.ActiveConversations.Id)
                    .ToList();

                if (channels.Count == 0)
                {
                    continue;
                }

                _logger.Log(LogSeverity.Info, $"Cleaning up conversations for {guild.Name}; found {channels.Count} channels");

                foreach (var c in channels)
                {
                    var message = (await c.GetMessagesAsync(1).FlattenAsync())
                        .FirstOrDefault();

                    if (message == null)
                    {
                        // Likely cannot happen since only admins can delete the opening message.
                        // Delete the channel, there's no history to preserve.
                        _logger.Log(LogSeverity.Info, $"It's so quiet in #{c.Name} that it might as well not exist anymore... so it doesn't.", logChannel);
                        await c.DeleteAsync();
                    } else
                    {
                        if (message.CreatedAt.AddDays(1) <= DateTime.Now)
                        {
                            _logger.Log(LogSeverity.Info, $"The {c.Mention} conversation died, moving it to the graveyard.", logChannel);
                            await c.ModifyAsync(p => p.CategoryId = categories.InactiveConversations.Id);
                        }
                    }
                }
            }
        }

        private async Task<ConversationCategories> GetConversationCategories(IGuild guild)
        {
            // Check for and, if necessary, create Categories.
            var categories = await guild.GetCategoriesAsync();
            var activeCategory = categories.FirstOrDefault(c => string.Compare(c.Name, ACTIVE_CATEGORY_NAME) == 0);

            if (activeCategory == null)
            {
                activeCategory = await guild.CreateCategoryAsync(ACTIVE_CATEGORY_NAME);
            }

            var inactiveCategory = categories.FirstOrDefault(c => string.Compare(c.Name, INACTIVE_CATEGORY_NAME) == 0);

            if (inactiveCategory == null)
            {
                inactiveCategory = await guild.CreateCategoryAsync(INACTIVE_CATEGORY_NAME);
            }

            return new ConversationCategories(activeCategory, inactiveCategory);
        }

        private void ValidateChannelName(string channelName)
        {
            if (channelName.IndexOf(' ') != -1)
            {
                throw new ArgumentException("Conversation name must be alphanumeric with no spaces.");
            }

            if (channelName.Length > MAX_CHANNEL_NAME_LENGTH)
            {
                throw new ArgumentException("Conversation name must be less than 20 characters.");
            }
        }

        private async Task SendConversationNotice(IMessageChannel channel, string message)
        {
            var embed = new EmbedBuilder()
            {
                Description = message,
            };

            _logger.Log(LogSeverity.Info, $"Channel Manager: {message}");

            await channel.SendMessageAsync(String.Empty, embed: embed.Build());
        }

        private async Task<ITextChannel> GetLogChannel(IGuild guild)
        {
            var channels = await guild.GetTextChannelsAsync();
            var logChannel = channels
                .FirstOrDefault(c => c.Name == "bot_log");

            return logChannel;
        }


        protected class ConversationCategories
        {
            public ICategoryChannel ActiveConversations { get; set; }
            public ICategoryChannel InactiveConversations { get; set; }

            public ConversationCategories(ICategoryChannel activeConversations, ICategoryChannel inactiveConversations)
            {
                ActiveConversations = activeConversations;
                InactiveConversations = inactiveConversations;
            }
        }

    }
}
