using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CssOptimizer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CssOptimizer.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/v2/optimize")]
    public class CustomOptimizeController : Controller
    {
        private readonly ICustomOptimizeCssService _customOptimizeCssService;

        public CustomOptimizeController(ICustomOptimizeCssService customOptimizeCssService)
        {
            _customOptimizeCssService = customOptimizeCssService ??
                                        throw new ArgumentNullException(nameof(customOptimizeCssService));
        }

        [HttpGet]
        [Route("css")]
        public async Task<IActionResult> OptimizeCss([FromQuery]string url)
        {
            var result = await _customOptimizeCssService.OptimizeCssAsync(url);

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
            var result = await _customOptimizeCssService.OptimizeCssInParallelAsync(urls);

            if (!result.IsSuccess)
            {
                return BadRequest(result.ValidationErrors);
            }

            return Json(result.Items);
        }
    }
}