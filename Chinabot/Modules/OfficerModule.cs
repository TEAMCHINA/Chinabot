using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Chinabot.Logging;
using Chinabot.Managers;
using Discord.WebSocket;
using System.Linq;

namespace Chinabot.Modules
{
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [Summary("Restricted Commands")]
    public class OfficerModule : ModuleBase
    {
        private ILogger _logger;
        private IAudioManager _audioManager;
        private readonly IChannelManager _channelManager;

        public OfficerModule(ILogger logger, IAudioManager audioManager, IChannelManager channelManager)
        {
            _logger = logger;
            _audioManager = audioManager;
            _channelManager = channelManager;
        }

        [Command("leave")]
        [Summary("Instructs the bot to leave the audio channels for the current guild.")]
        public async Task Leave()
        {
            if (Context.Guild == null)
            {
                await ReplyAsync("This command can only be ran in a server.");
                return;
            }

            await _audioManager.LeaveAudioChannels(Context.Guild);
        }

        [Command("say")]
        [Alias("echo")]
        [Summary("Echos the provided input")]
        public async Task Say([Remainder] string input)
        {
            await _audioManager.Speak(Context.Guild, input);
        }

        [Command("join", RunMode = RunMode.Async)]
        [Summary("Instructs the bot to join the current users audio channel.")]
        public async Task Join()
        {
            if (Context.Guild == null)
            {
                await ReplyAsync("This command can only be ran in a server.");
                return;
            }

            var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;

            if (channel != null)
            {
                await _audioManager.JoinAudioChannel(Context.Guild, channel);
            } else
            {
                // User wasn't in a channel so join the default audio channel.
                await _audioManager.JoinDefaultAudioChannel(Context.Guild);
            }
        }

        #region Conversation Commands
        [Command("startchat", RunMode = RunMode.Async)]
        [Summary("Creates a 'dedicated channel for conversations. First argument is the channel name, everything else is the first message.")]
        public async Task CreateConversation([Remainder] string input)
        {
            await _channelManager.CreateConversationChannel(Context, input);
        }

        [Command("promotechat", RunMode = RunMode.Async)]
        [Summary("Promotes a 'conversation' channel up a level (inactive > active, active > real boy).")]
        public async Task Promote([Remainder] string input)
        {
            await _channelManager.PromoteChannel(Context, input);
        }

        [Command("demotechat", RunMode = RunMode.Async)]
        [Summary("Demotes a 'conversation' channel from active to inactive.")]
        public async Task Demote([Remainder] string input)
        {
            await _channelManager.DemoteChannel(Context, input);
        }
        #endregion

        #region Audio Commands
        [Command("airhorn", RunMode = RunMode.Async)]
        [Summary("Plays an airhorn sound. Duh.")]
        public async Task Airhorn()
        {
            if (UserHasPermission())
            {
                await _audioManager.SendAudioAsync(Context.Guild, "Audio\\airhorn.mp3");
            }
        }

        [Command("price", RunMode = RunMode.Async)]
        [Summary("The Price is Wrong, bitch.")]
        public async Task Price()
        {
            if (UserHasPermission())
            {
                await _audioManager.SendAudioAsync(Context.Guild, "Audio\\price_is_right.mp3");
            }
        }

        [Command("xfiles", RunMode = RunMode.Async)]
        [Summary("The truth is out there...")]
        public async Task Xfiles()
        {
            if (UserHasPermission())
            {
                await _audioManager.SendAudioAsync(Context.Guild, "Audio\\x-files.mp3");
            }
        }

        [Command("omaewa", RunMode = RunMode.Async)]
        [Summary("You are already dead.")]
        public async Task Omaewa()
        {
            if (UserHasPermission())
            {
                await _audioManager.SendAudioAsync(Context.Guild, "Audio\\omaewa.mp3");
            }
        }

        [Command("nina", RunMode = RunMode.Async)]
        [Summary("You are already... Nina?!?")]
        public async Task Nina()
        {
            if (UserHasPermission())
            {
                await _audioManager.SendAudioAsync(Context.Guild, "Audio\\nina.mp3");
            }
        }

        [Command("curb", RunMode = RunMode.Async)]
        [Summary("Don't get too excited.")]
        public async Task Curb()
        {
            if (UserHasPermission())
            {
                await _audioManager.SendAudioAsync(Context.Guild, "Audio\\curb.mp3");
            }
        }

        [Command("cupu", RunMode = RunMode.Async)]
        [Summary("Congratulations...")]
        public async Task Cupu()
        {
            if (UserHasPermission())
            {
                await _audioManager.SendAudioAsync(Context.Guild, "Audio\\cupu.mp3");
            }
        }

        [Command("law", RunMode = RunMode.Async)]
        [Summary("I didn't order this...")]
        public async Task Law()
        {
            if (UserHasPermission())
            {
                await _audioManager.SendAudioAsync(Context.Guild, "Audio\\law.mp3");
            }
        }

        [Command("doesntmatter", RunMode = RunMode.Async)]
        [Summary("It doesn't matter what you think!")]
        public async Task ItDoesntMatter()
        {
            if (UserHasPermission())
            {
                await _audioManager.SendAudioAsync(Context.Guild, "Audio\\it_doesnt_matter.mp3");
            }
        }

        [Command("knowyourrole", RunMode = RunMode.Async)]
        [Summary("You know your damn role...")]
        public async Task KnowYourRole()
        {
            if (UserHasPermission())
            {
                await _audioManager.SendAudioAsync(Context.Guild, "Audio\\know_your_role.mp3");
            }
        }

        [Command("cena", RunMode = RunMode.Async)]
        [Summary("And his name was...")]
        public async Task Cena()
        {
            if (UserHasPermission())
            {
                await _audioManager.SendAudioAsync(Context.Guild, "Audio\\cena.mp3");
            }
        }

        [Command("tonightyou", RunMode = RunMode.Async)]
        [Summary("Tonight... you...")]
        public async Task TonightNow()
        {
            if (UserHasPermission())
            {
                await _audioManager.SendAudioAsync(Context.Guild, "Audio\\tonightyou.mp3");
            }
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Plays the supplied audio file.")]
        public async Task Play([Remainder]string input)
        {
            var user = Context.User as SocketGuildUser;
            if (user.Id == 147847182752415744) // Only let me run this for now.
            {
                await _audioManager.SendAudioAsync(Context.Guild, input);
            } else
            {
                throw new TaskCanceledException($"{user.Nickname} does not have permission to execute this command.");
            }
        }
        #endregion

        private bool UserHasPermission()
        {
            var user = Context.User as SocketGuildUser;

            // return user.GuildPermissions.Administrator;
            return true;
        }
    }
}
