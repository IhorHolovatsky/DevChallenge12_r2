using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CssOptimizer.Services.ChromeServices;
using Microsoft.AspNetCore.Http;
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