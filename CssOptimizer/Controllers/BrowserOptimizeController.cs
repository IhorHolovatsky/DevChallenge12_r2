using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CssOptimizer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CssOptimizer.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/v1/optimize")]
    public class BrowserOptimizeController : Controller
    {
        private readonly IBrowserOptimizeCssService _browserOptimizeCssService;

        public BrowserOptimizeController(IBrowserOptimizeCssService browserOptimizeCssService)
        {
            _browserOptimizeCssService = browserOptimizeCssService ?? throw new ArgumentNullException();
        }

        [HttpGet]
        [Route("css")]
        public async Task<IActionResult> OptimizeCss([FromQuery]string url)
        {
            var result  = await _browserOptimizeCssService.OptimizeCssAsync(url);

            if (!result.IsSuccess)
            {
                return BadRequest(result.ValidationErrors);
            }

            return Content(result.Items);
        }
        
        [HttpPost]
        [Route("css/parallel")]
        public async Task<IActionResult> OptimizeCssParallel([FromBody]List<string> urls)
        {
            var result = await _browserOptimizeCssService.OptimizeCssInParallelAsync(urls);

            if (!result.IsSuccess)
            {
                return BadRequest(result.ValidationErrors);
            }

            return Json(result.Items);
        }
    }
}