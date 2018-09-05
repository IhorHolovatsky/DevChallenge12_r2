using System.Collections.Generic;
using System.Threading.Tasks;
using CssOptimizer.Services.ChromeServices;
using Microsoft.AspNetCore.Mvc;

namespace CssOptimizer.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/chrome")]
    public class ChromeController : Controller
    {
        [HttpGet]
        [Route("sessions")]
        public Task<IEnumerable<ChromeSession>> GetActiveSessions()
        {
            return ChromeSessionPool.GetActiveSessions();
        }
    }
}