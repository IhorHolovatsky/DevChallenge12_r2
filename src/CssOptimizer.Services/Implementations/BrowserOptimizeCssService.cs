using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaristaLabs.ChromeDevTools.Runtime.CSS;
using BaristaLabs.ChromeDevTools.Runtime.Page;
using CDN.Domain.Constants;
using CssOptimizer.Domain.Configuration;
using CssOptimizer.Domain.Validation;
using CssOptimizer.Services.ChromeServices;
using CssOptimizer.Services.Interfaces;
using CssOptimizer.Services.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUglify;
using CSS = BaristaLabs.ChromeDevTools.Runtime.CSS;
using DOM = BaristaLabs.ChromeDevTools.Runtime.DOM;

namespace CssOptimizer.Services.Implementations
{
    public class BrowserOptimizeCssService : IBrowserOptimizeCssService
    {
        private readonly IMemoryCache _cache;
        private readonly CacheConfiguration _cacheConfiguration;

        public BrowserOptimizeCssService(IMemoryCache cache,
                                         IOptions<CacheConfiguration> cachOptionsAccessor)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _cacheConfiguration = cachOptionsAccessor?.Value ?? throw new ArgumentNullException(nameof(cachOptionsAccessor));
        }
        
        /// <inheritdoc />
        public async Task<ResponseWrapper<string>> OptimizeCssAsync(string url)
        {
            var result = new ResponseWrapper<string>
            {
                ValidationErrors = ValidateUrls(new List<string> { url })
            };

            if (!result.IsSuccess)
            {
                return result;
            }

            var minCss = await GetMinCssForUrlAsync(url);

            result.Items = minCss;
            return result;
        }
        
        /// <inheritdoc />
        public Task<ResponseWrapper<Dictionary<string, string>>> OptimizeCssInParallelAsync(List<string> urls)
        {
            //skip duplicates
            urls = urls.Distinct().ToList();

            var result = new ResponseWrapper<Dictionary<string, string>>
            {
                ValidationErrors = ValidateUrls(urls)
            };

            if (!result.IsSuccess)
            {
                return Task.FromResult(result);
            }

            var tasks = new Dictionary<string, Task<string>>();

            foreach (var url in urls)
            {
                tasks.Add(url, GetMinCssForUrlAsync(url));
            }

            Task.WaitAll(tasks.Values.ToArray());

            result.Items = tasks.ToDictionary(t => t.Key, t => t.Value.Result);
            return Task.FromResult(result);
        }


        #region Private members

        private async Task<string> GetMinCssForUrlAsync(string url)
        {
            //Take from cache if exists
            if (_cache.Get<string>(url) != null)
            {
                return _cache.Get<string>(url);
            }

            //Take available session from pool
            var chromeSession = ChromeSessionPool.GetInstance();

            try
            {
                //Navigate to page, enable and start rule usage tracking
                await chromeSession.InternalSession.DOM.Enable(new DOM.EnableCommand());
                await chromeSession.InternalSession.CSS.Enable(new CSS.EnableCommand());
                await chromeSession.InternalSession.CSS.StartRuleUsageTracking(new CSS.StartRuleUsageTrackingCommand());
                await chromeSession.InternalSession.Page.Navigate(new NavigateCommand { Url = url });

                var rules = new List<RuleUsage>();
                TakeCoverageDeltaCommandResponse coverageResponse;


                //TODO: smart logic to wait report, currently just wait when delta == 0
                var waitCount = 0;
                do
                {
                    //https://chromedevtools.github.io/devtools-protocol/tot/CSS#method-takeCoverageDelta
                    coverageResponse = await chromeSession.InternalSession.CSS.TakeCoverageDelta(new TakeCoverageDeltaCommand());
                    coverageResponse.Coverage = coverageResponse.Coverage ?? new RuleUsage[0];

                    //If no found.. sleep and try it again
                    if (coverageResponse.Coverage.Length == 0)
                    {
                        Thread.Sleep(++waitCount * 100);

                        //try again after sleep
                        //timeout 120 seconds
                        coverageResponse = await chromeSession.InternalSession.CSS.TakeCoverageDelta(new TakeCoverageDeltaCommand());
                        coverageResponse.Coverage = coverageResponse.Coverage ?? new RuleUsage[0];

                        rules.AddRange(coverageResponse.Coverage);

                        //if found, reset sleep count, because it means that chrome is processing css
                        if (coverageResponse.Coverage.Length > 0)
                            waitCount = 0;
                    }
                }
                while (coverageResponse.Coverage.Length != 0
                       || waitCount <= 3);

                //Stop tacking
                await chromeSession.InternalSession.CSS.StopRuleUsageTracking(new CSS.StopRuleUsageTrackingCommand());

                //Get stylesheets
                var usedRules = rules.Where(r => r.Used).ToList();
                var styleSheetIds = usedRules.Select(r => r.StyleSheetId).Distinct();
                var styleSheets = styleSheetIds.Select(s => new
                {
                    chromeSession.InternalSession.CSS.GetStyleSheetText(new GetStyleSheetTextCommand { StyleSheetId = s }).Result.Text,
                    StyleSheetId = s
                }).ToList();


                //Get only used css, and constrcut it
                var usedCssStrb = new StringBuilder();
                foreach (var rule in usedRules)
                {
                    var styleSheetText = styleSheets.First(s => s.StyleSheetId.Equals(rule.StyleSheetId)).Text;

                    var ruleString = styleSheetText.Substring((int)rule.StartOffset, (int)rule.EndOffset - (int)rule.StartOffset);
                    usedCssStrb.Append(ruleString);
                }

                //minimize CSS
                var minCss = Uglify.Css(usedCssStrb.ToString()).Code;

                //Cache it for one hour
                _cache.Set(url, minCss, TimeSpan.FromSeconds(_cacheConfiguration.UrlCacheTime));

                return minCss;
            }
            finally
            {
                //Free up resource
                ChromeSessionPool.ReleaseInstance(chromeSession);
            }
        }

        private ResponseErrors ValidateUrls(List<string> urls)
        {
            var errors = new ResponseErrors();

            foreach (var url in urls)
            {
                //Validate if URL is correct
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    errors.Add(new ResponseError(RequestErrorCodes.INVALID_REQUEST_URL_PARAMETER, $"Invalid URL '{url}'"));
                }
            }

            return errors;
        }

        #endregion
    }
}