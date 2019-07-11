using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace ChinaBotService.Services
{
    public class SearchService : ISearchService
    {
        const string SearchBaseUrl = "https://www.googleapis.com/customsearch/v1";
        static HttpClient _client = new HttpClient();

        public async Task<string> GetImageByQuery(string searchQuery)
        {
            var query = new Dictionary<string, string>();
            query.Add("cx", ConfigurationManager.AppSettings["GoogleEngineCode"]);
            query.Add("key", ConfigurationManager.AppSettings["GoogleApiKey"]);
            query.Add("q", HttpUtility.UrlEncode(searchQuery));
            query.Add("safe", "active");
            query.Add("searchType", "image");

            var queryString = string.Join(
                "&",
                query
                    .Select(kvp => $"{kvp.Key}={kvp.Value}"));

            var searchUrl = $"{SearchBaseUrl}?{queryString}";
            var response = await _client.GetAsync(searchUrl);
            var responseString = await response.Content.ReadAsStringAsync();

            dynamic responseObject = JObject.Parse(responseString);
            dynamic items = responseObject.items;

            if (items == null)
            {
                return string.Empty;
            }

            dynamic item = items[0];

            return item.link;
        }
    }
}