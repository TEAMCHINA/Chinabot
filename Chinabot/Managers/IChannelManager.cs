using Discord.Commands;
using System.Threading.Tasks;

namespace Chinabot.Managers
{
    public interface IChannelManager
    {
        Task CreateConversationChannel(ICommandContext context, string input);
        Task PromoteChannel(ICommandContext context, string input);
        Task DemoteChannel(ICommandContext context, string input);
    }
}