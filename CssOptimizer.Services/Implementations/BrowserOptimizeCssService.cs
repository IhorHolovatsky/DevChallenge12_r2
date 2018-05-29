using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CDN.Domain.Constants;
using CssOptimizer.Domain.Validation;
using CssOptimizer.Services.ChromeServices;
using CssOptimizer.Services.Interfaces;
using MasterDevs.ChromeDevTools;
using MasterDevs.ChromeDevTools.Protocol.Chrome.CSS;
using MasterDevs.ChromeDevTools.Protocol.Chrome.Page;
using Microsoft.Extensions.Caching.Memory;
using NUglify;
using CacheExtensions = CssOptimizer.Services.Utils.CacheExtensions;
using CSS = MasterDevs.ChromeDevTools.Protocol.Chrome.CSS;
using DOM = MasterDevs.ChromeDevTools.Protocol.Chrome.DOM;

namespace CssOptimizer.Services.Implementations
{
    public class BrowserOptimizeCssService : IBrowserOptimizeCssService
    {
        private readonly IMemoryCache _cache;

        public BrowserOptimizeCssService(IMemoryCache cache)
        {
            _cache = cache;
        }

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

        public Task<ResponseWrapper<Dictionary<string, string>>> OptimizeCssInParallelAsync(List<string> urls)
        {
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
                await chromeSession.SendAsync(new DOM.EnableCommand());
                await chromeSession.SendAsync(new CSS.EnableCommand());
                await chromeSession.SendAsync(new CSS.StartRuleUsageTrackingCommand());
                await chromeSession.SendAsync(new NavigateCommand { Url = url });

                var rules = new List<RuleUsage>();
                CommandResponse<TakeCoverageDeltaCommandResponse> coverageResponse;


                //TODO: smart logic to wait report, currently just wait when delta == 0
                var waitCount = 0;
                do
                {
                    //https://chromedevtools.github.io/devtools-protocol/tot/CSS#method-takeCoverageDelta
                    coverageResponse = await chromeSession.SendAsync(new TakeCoverageDeltaCommand());
                    rules.AddRange(coverageResponse.Result.Coverage);

                    //If no found.. sleep and try it again
                    if (coverageResponse.Result.Coverage.Length == 0)
                    {
                        Thread.Sleep(++waitCount * 100);

                        //try again after sleep
                        coverageResponse = await chromeSession.SendAsync(new TakeCoverageDeltaCommand());
                        rules.AddRange(coverageResponse.Result.Coverage);

                        //if found, reset sleep count, because it means that chrome is processing css
                        if (coverageResponse.Result.Coverage.Length > 0)
                            waitCount = 0;
                    }
                }
                while (coverageResponse.Result.Coverage.Length != 0
                       || waitCount <= 3);

                //Stop tacking
                await chromeSession.SendAsync(new CSS.StopRuleUsageTrackingCommand());

                //Get stylesheets
                var usedRules = rules.Where(r => r.Used).ToList();
                var styleSheetIds = usedRules.Select(r => r.StyleSheetId).Distinct();
                var styleSheets = styleSheetIds.Select(s => new
                {
                    chromeSession.SendAsync(new GetStyleSheetTextCommand { StyleSheetId = s }).Result.Result.Text,
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
                CacheExtensions.Set(_cache, url, minCss, TimeSpan.FromHours(1));

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