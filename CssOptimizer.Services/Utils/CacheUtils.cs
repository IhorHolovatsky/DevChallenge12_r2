using System;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace CssOptimizer.Services.Utils
{
    public static class CacheExtensions
    {
        private static CancellationTokenSource _resetCacheToken = new CancellationTokenSource();
        
        public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, TimeSpan absoluteExpirationRelativeToNow)
        {
            var entry = cache.CreateEntry(key);
            entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
            entry.Value = value;
            entry.AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token));
            entry.Dispose();

            return value;
        }

        public static void Reset(this IMemoryCache cache)
        {
            if (_resetCacheToken != null && !_resetCacheToken.IsCancellationRequested && _resetCacheToken.Token.CanBeCanceled)
            {
                _resetCacheToken.Cancel();
                _resetCacheToken.Dispose();
            }

            _resetCacheToken = new CancellationTokenSource();
        }
    }
}