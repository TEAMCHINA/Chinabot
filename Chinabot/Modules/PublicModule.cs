using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net.Http;
using Chinabot.Logging;
using System.Threading;

namespace Chinabot.Modules
{
    [Summary("Public Commands")]
    public class PublicModule : ModuleBase
    {
        private const string _serviceUrlBase = "http://tvpc/api";
        private readonly HttpClient _httpClient;
        private CommandService _service;
        private ILogger _logger;

        public PublicModule(CommandService service, ILogger logger)
        {
            _service = service;
            _httpClient = new HttpClient();
            _logger = logger;
        }

        [Command("invite")]
        [Summary("Returns the OAuth2 Invite URL of the bot")]
        public async Task Invite()
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            await ReplyAsync(
                $"A user with `MANAGE_SERVER` can invite me to your server here: <https://discordapp.com/oauth2/authorize?client_id={application.Id}&scope=bot>");
        }

        [Command("info")]
        [Summary("Server info (ie. uptime, memory usage, etc.)")]
        public async Task Info()
        {
            var application = await Context.Client.GetApplicationInfoAsync();
            await ReplyAsync(
                $"{Format.Bold("Info")}\n" +
                $"- Author: {application.Owner.Username} (ID {application.Owner.Id})\n" +
                $"- Library: Discord.Net ({DiscordConfig.Version})\n" +
                $"- Runtime: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}\n" +
                $"- Uptime: {GetUptime()}\n\n" +

                $"{Format.Bold("Stats")}\n" +
                $"- Heap Size: {GetHeapSize()} MB\n" +
                $"- Guilds: {(Context.Client as DiscordSocketClient).Guilds.Count}\n" +
                $"- Channels: {(Context.Client as DiscordSocketClient).Guilds.Sum(g => g.Channels.Count)}\n" +
                $"- Users: {(Context.Client as DiscordSocketClient).Guilds.Sum(g => g.Users.Count)}"
            );
        }

        [Command("image")]
        [Summary("Looks up an image and displays the results.")]
        public async Task Image([Remainder] string input)
        {
            CheckUserPermissions();

            var searchUrl = $"{_serviceUrlBase}/image?searchQuery={input}";
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.GetAsync(searchUrl);
            }
            catch (HttpRequestException e)
            {
                throw new HttpRequestException("The image lookup service failed or is not listening.");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var imageUrl = responseString;

            var application = await Context.Client.GetApplicationInfoAsync();

            var embed = new EmbedBuilder()
            {
                Title = $"{input}",
                ImageUrl = imageUrl,
                Url = imageUrl,
                Description = $"<@{Context.Message.Author.Id}>",
            };

            _logger.Log(LogSeverity.Info, $"Returned image URL: {imageUrl}");

            await ReplyAsync(String.Empty, embed: embed.Build());
        }

        private Task ThrowAsync<T>()
        {
            TaskCompletionSource<IUserMessage> tcs = new TaskCompletionSource<IUserMessage>();
            tcs.SetCanceled();

            return tcs.Task;
        }

        [Command("help")]
        [Summary("You're beyond any.")]
        public async Task Help()
        {
            var prefix = "!";

            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "These are the commands you can use:"
            };

            foreach (var module in _service.Modules)
            {
                string description = null;
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                        description += $"{prefix}{cmd.Aliases.First()} - {cmd.Summary}\n";
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Summary;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }

            var user = Context.User as SocketGuildUser;
            await user.SendMessageAsync("", false, builder.Build());
        }

        private static string GetUptime()
            => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");
        private static string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString();

        private void CheckUserPermissions()
        {
            var user = Context.User as SocketGuildUser;
            var role = (user as IGuildUser).Guild.Roles.FirstOrDefault(x => x.Name == "Untrusted Loser");
            if (!user.GuildPermissions.Administrator && user.Roles.Contains(role))
            {
                throw new UnauthorizedAccessException();
            }
        }
    }
}
