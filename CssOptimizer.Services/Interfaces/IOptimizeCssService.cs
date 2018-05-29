using System.Collections.Generic;
using System.Threading.Tasks;
using CssOptimizer.Domain.Validation;

namespace CssOptimizer.Services.Interfaces
{
    public interface IOptimizeCssService
    {
        Task<ResponseWrapper<string>> OptimizeCssAsync(string url);
        Task<ResponseWrapper<Dictionary<string,string>>> OptimizeCssInParallelAsync(List<string> url);
    }
}