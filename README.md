# Chinabot
Discord bot prototype leveraging the [Discord.Net](https://www.nuget.org/packages/Discord.Net)
library, an asynchronous API wrapper for Discord. This was as much an educational experiment
as the library is open sourced and not particularly well documented and as, hopefully, an easy
to follow example demonstrating basic functionality (executing commands, joining servers and
voice channels, playing audio and sending messages to the server.)

## Commands
Command processing is managed in Chinabot.CommandHandler which creates a CommandContext. The
commands themselves are defined inside of the Modules namespace. The commands defined in the
"OfficerModule" require the user attempting to execute them to have the "ManageGuild" permission
on the server, as defined in the RequireUserPermission attribute. The commands defined in the
public module has no restrictions for execution.

## Playing Audio Files
Playing audio files follows the examples described in
[Sending Voice](https://discord.foxbot.me/docs/guides/voice/sending-voice.html)
