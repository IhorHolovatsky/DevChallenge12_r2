using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CssOptimizer.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/v2/optimize")]
    public class CustomOptimizeController : Controller
    {
        [HttpGet]
        [Route("css")]
        public IActionResult OptimizeCss([FromQuery]string url)
        {
            return Ok();
        }
        
        [HttpPost]
        [Route("css/parallel")]
        public IActionResult OptimizeCssParallel([FromBody]List<string> url)
        {
            return Ok();
        }
    }
}