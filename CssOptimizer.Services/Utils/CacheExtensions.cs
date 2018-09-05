using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace CssOptimizer.Services.Utils
{
    public static class CacheExtensions
    {
        private static readonly List<string> _cachedObjectKeys = new List<string>();
        
        public static TItem Set<TItem>(this IMemoryCache cache, string key, TItem value, TimeSpan absoluteExpirationRelativeToNow)
        {
            var entry = cache.CreateEntry(key);
            entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
            entry.Value = value;
            entry.Dispose();

            //ASP.NET Core implementation doesn't have functionality to reset cache. 
            //So do here workaround (wrapper)
            _cachedObjectKeys.Add(key);

            return value;
        }

        public static void Reset(this IMemoryCache cache)
        {
            foreach (var cachedObjectKey in _cachedObjectKeys)
            {
                cache.Remove(cachedObjectKey);
            }
            
            _cachedObjectKeys.Clear();
        }

        public static void Reset(this IMemoryCache cache, string objectKey)
        {
            var urlToReset = _cachedObjectKeys.FirstOrDefault(k => k.Equals(objectKey, StringComparison.CurrentCultureIgnoreCase));

            if (!string.IsNullOrEmpty(urlToReset))
            {
                cache.Remove(urlToReset);
            }
        }
    }
}