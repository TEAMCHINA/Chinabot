using System.Threading.Tasks;

namespace ChinaBotService.Services
{
    public interface ISearchService
    {
        Task<string> GetImageByQuery(string query);
    }
}
