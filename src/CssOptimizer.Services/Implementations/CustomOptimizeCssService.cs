using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CDN.Domain.Constants;
using CssOptimizer.Domain.Configuration;
using CssOptimizer.Domain.Validation;
using CssOptimizer.Services.Interfaces;
using CssOptimizer.Services.Utils;
using ExCSS;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUglify;

namespace CssOptimizer.Services.Implementations
{
    public class CustomOptimizeCssService : ICustomOptimizeCssService
    {
        private readonly IMemoryCache _cache;
        private readonly CacheConfiguration _cacheConfiguration;

        public CustomOptimizeCssService(IMemoryCache cache,
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
            //skip duplicate urls
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


        #region Private methods

        /// <summary>
        /// Calculate only used css stytyles for given url.
        /// Contains cache logic.
        /// </summary>
        private async Task<string> GetMinCssForUrlAsync(string url)
        {
            //Take from cache if exists
            if (_cache.Get<string>(url) != null)
            {
                return _cache.Get<string>(url);
            }

            //create uri, it's safe, since we did validation
            Uri.TryCreate(url, UriKind.Absolute, out var uri);

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                //In case of redirect, return nothing
                if (IsRedirectCode(response.StatusCode))
                {
                    return string.Empty;
                }

                //Container for accumilating all CSS styles of page
                var cssFullStrb = new StringBuilder();
                var htmlParser = new HtmlParser();
                var document = htmlParser.Parse(await response.Content.ReadAsStringAsync());
                
                var cssLinks = document.QuerySelectorAll("link")
                    .Cast<IHtmlLinkElement>()
                    .Where(l => (l?.Relation?.Equals("stylesheet", StringComparison.CurrentCultureIgnoreCase) ?? false)
                                || (l?.Type?.Equals("text/css", StringComparison.CurrentCultureIgnoreCase) ?? false))
                    .ToList();
                var cssStyles = document.QuerySelectorAll("style");

                #region Load external css files in parallel
                var loadCssTasks = new List<Task>();

                foreach (var cssLink in cssLinks)
                {
                    loadCssTasks.Add(Task.Factory.StartNew(() =>
                    {
                        using (var httpClientForCss = new HttpClient())
                        {
                            var link = cssLink?.Attributes?["href"]?.Value;

                            if (string.IsNullOrEmpty(link))
                            {
                                return;
                            }

                            //Some sites could use relative links, or other shit
                            if (link.StartsWith("//"))
                                link = "https:" + link;

                            if (Uri.IsWellFormedUriString(link, UriKind.Relative))
                                link = $"{uri.Scheme}://{uri.Authority}/" + link;

                            //If link is not valid (even on their site, just skip it
                            if (!Uri.IsWellFormedUriString(link, UriKind.Absolute))
                            {
                                return;
                            }

                            var cssResponse = httpClientForCss.GetAsync(link).Result;
                            cssFullStrb.Append(cssResponse.Content.ReadAsStringAsync().Result);
                        }
                    }));
                }

                Task.WaitAll(loadCssTasks.ToArray());
                #endregion
                
                if (cssStyles != null)
                {
                    foreach (var cssStyle in cssStyles.Select(c => c.InnerHtml).ToList())
                    {
                        cssFullStrb.Append(cssStyle);
                    }
                }

                var cssParser = new Parser();
                var cssRules = cssParser.Parse(cssFullStrb.ToString());

                var usedRules = cssRules.StyleRules.Where(r =>
                                                          {
                                                              var angleSharpCssParser = new AngleSharp.Parser.Css.CssParser();
                                                              var cssSelectorString = r.Selector.ToString();
                                                              var cssSelectorParsed = angleSharpCssParser.ParseSelector(cssSelectorString);

                                                              //Validate selector. 
                                                              //Not all selectors are supported... this is a disadvantage of this approach
                                                              if (cssSelectorParsed == null
                                                                  || cssSelectorParsed.GetType().Name.Contains("UnknownSelector"))
                                                              {
                                                                  return true;

                                                              }

                                                              //The idea is that if there is at least one element by given cssSelector, that means that it's used
                                                              var cssUsage = document.QuerySelector(cssSelectorString);
                                                              return cssUsage != null;
                                                          })
                    .Select(r => r.ToString())
                    .ToList();

                //Combine and minify all used rules
                var minCss = Uglify.Css(string.Join(string.Empty, usedRules)).Code;

                //Cache it for one hour
                _cache.Set(url, minCss, TimeSpan.FromSeconds(_cacheConfiguration.UrlCacheTime));

                return minCss;
            }
        }

        /// <summary>
        /// Check if url is valid URI
        /// </summary>
        private ResponseErrors ValidateUrls(List<string> urls)
        {
            var errors = new ResponseErrors();

            foreach (var url in urls)
            {
                //Validate if URL is correct
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    errors.Add(new ResponseError(RequestErrorCodes.INVALID_REQUEST_URL_PARAMETER, $"Invalid URL '{url}'. Example of valid url 'https://google.com/'"));
                }
            }

            return errors;
        }

        /// <summary>
        /// Check if http status code is one of the redirect codes
        /// </summary>
        private bool IsRedirectCode(HttpStatusCode statusCode)
        {
            //Http Status code: 3xx = Redirection
            return (int) statusCode >= 300
                   && (int) statusCode < 400;
        }

        #endregion
    }
}