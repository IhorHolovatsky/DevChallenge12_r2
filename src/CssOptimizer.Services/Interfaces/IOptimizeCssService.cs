using System.Collections.Generic;
using System.Threading.Tasks;
using CssOptimizer.Domain.Validation;

namespace CssOptimizer.Services.Interfaces
{
    public interface IOptimizeCssService
    {
        /// <summary>
        /// Calculate only used CSS styles for given url.
        /// </summary>
        /// <param name="url">URL to analyze</param>
        /// <returns>minimal CSS styles which are required to render current url</returns>
        Task<ResponseWrapper<string>> OptimizeCssAsync(string url);


        /// <summary>
        /// Calculate only used CSS styles for given list of urls in parallel.
        /// </summary>
        /// <param name="urls">URLs to analyze</param>
        /// <returns>minimal CSS styles which are required to render urls</returns>
        Task<ResponseWrapper<Dictionary<string,string>>> OptimizeCssInParallelAsync(List<string> urls);
    }
}