using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CssOptimizer.Services.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace CssOptimizer.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/cache")]
    public class CacheController : Controller
    {
        private readonly IMemoryCache _cache;

        public CacheController(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [HttpGet]
        [Route("reset")]
        public IActionResult Reset()
        {
            _cache.Reset();

            return Ok();
        }
    }
}