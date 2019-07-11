﻿using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Chinabot.Logging;
using Chinabot.Managers;

namespace Chinabot.Modules
{
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public class OfficerModule : ModuleBase
    {
        private ILogger _logger;
        private IAudioManager _audioManager;

        public OfficerModule(ILogger logger, IAudioManager audioManager)
        {
            _logger = logger;
            _audioManager = audioManager;
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

        [Command("airhorn", RunMode = RunMode.Async)]
        [Summary("Plays an airhorn sound. Duh.")]
        public async Task Airhorn()
        {
            if (Context.Message.Author.Username == "TEAMCHINA")
            {
                await _audioManager.SendAudioAsync(Context.Guild, "Audio\\airhorn.mp3");
            }
        }

        [Command("cena", RunMode = RunMode.Async)]
        [Summary("And his name was...")]
        public async Task Cena()
        {
            if (Context.Message.Author.Username == "TEAMCHINA")
            {
                await _audioManager.SendAudioAsync(Context.Guild, "Audio\\cena.mp3");
            }
        }
    }
}
