using ChinaBotService.Services;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace ChinaBotService.Controllers
{
    public class ImageController : ApiController
    {
        private ISearchService _searchService;

        public ImageController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        // GET: Image
        public async Task<HttpResponseMessage> Get(string searchQuery)
        {
            var imageUrl = await _searchService.GetImageByQuery(searchQuery);

            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(imageUrl, Encoding.UTF8, "application/json");

            return response;
        }
    }
}