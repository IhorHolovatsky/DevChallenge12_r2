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
using CssOptimizer.Domain.Validation;
using CssOptimizer.Services.Interfaces;
using ExCSS;
using NUglify;

namespace CssOptimizer.Services.Implementations
{
    public class CustomOptimizeCssService : ICustomOptimizeCssService
    {
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
        {   //skip duplicates
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

        private async Task<string> GetMinCssForUrlAsync(string url)
        {
            Uri.TryCreate(url, UriKind.Absolute, out var uri);

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                //TODO all redirect codes
                if (response.StatusCode == HttpStatusCode.Redirect)
                {
                    return string.Empty;
                }

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
                            if (link.StartsWith("//"))
                                link = "https:" + link;

                            if (link.StartsWith("/"))
                                link = $"{uri.Scheme}://{uri.Authority}" + link;

                            var cssResponse = httpClientForCss.GetAsync(link).Result;
                            cssFullStrb.Append(cssResponse.Content.ReadAsStringAsync().Result);
                        }
                    }));
                }

                #endregion

                Task.WaitAll(loadCssTasks.ToArray());

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
                                                              try
                                                              {
                                                                  var cssUsage = document.QuerySelector(r.Selector.ToString());
                                                                  return cssUsage != null;
                                                              }
                                                              catch (Exception e)
                                                              {
                                                                  return true;
                                                              }
                                                          })
                    .Select(r => r.ToString())
                    .ToList();

                var minCss = Uglify.Css(string.Join(string.Empty, usedRules)).Code;

                return minCss;
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