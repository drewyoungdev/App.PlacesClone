using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PlaceApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PlacesController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult> GetBitField()
        {
            return this.Ok();
        }
    }
}
