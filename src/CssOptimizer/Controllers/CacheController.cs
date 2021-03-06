﻿using System;
using CssOptimizer.Services.Utils;
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

        [HttpDelete]
        [Route("reset")]
        public IActionResult Reset()
        {
            _cache.Reset();

            return Ok();
        }

        [HttpDelete]
        [Route("reset/url")]
        public IActionResult Reset(string url)
        {
            _cache.Reset(url);

            return Ok();
        }
    }
}