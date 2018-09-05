using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CssOptimizer.Domain.Configuration;
using CssOptimizer.Domain.Exceptions;

namespace CssOptimizer.Services.ChromeServices
{
    /// <summary>
    /// Represents chrome session pool. 
    /// </summary>
    public static class ChromeSessionPool
    {
        #region Config

        private static ChromeSessionPoolConfiguration _chromePoolConfiguration;

        #endregion

        private static Chrome _chromeProcess;
        private static readonly ConcurrentDictionary<ChromeSession, bool> ChromeSessions = new ConcurrentDictionary<ChromeSession, bool>();

        public static async Task InitPool(ChromeSessionPoolConfiguration config)
        {
            _chromeProcess = new Chrome(config.ChromeDebuggingPort, config.IsHeadlessMode);
            _chromePoolConfiguration = config;

            for (var i = 0; i < config.MaxSessionPoolCount; i++)
            {
                var chromeSession = await _chromeProcess.CreateNewSession();
                ChromeSessions.TryAdd(chromeSession, false);
            }
        }

        public static Task<IEnumerable<ChromeSession>> GetActiveSessions()
        {
            return Task.FromResult(ChromeSessions.Keys.AsEnumerable());
        }

        /// <summary>
        /// Get first available chrome session thread and mark it as 'InUse',
        /// it's important to call 'ReleaseInstance' after all work will be done
        /// </summary>
        public static ChromeSession GetInstance()
        {
            const bool inUse = true;
            ChromeSession chromeSession;

            lock (ChromeSessions)
            {
                chromeSession = ChromeSessions.FirstOrDefault(s => !s.Value).Key;

                if (chromeSession == null)
                {
                    #region Timeout logic

                    var timeoutTime = DateTime.Now.AddSeconds(_chromePoolConfiguration.RequestTimeout);

                    //Wait until timeout
                    while (DateTime.Now < timeoutTime)
                    {
                        Thread.Sleep(500);

                        chromeSession = ChromeSessions.FirstOrDefault(s => !s.Value).Key;

                        if (chromeSession != null)
                        {
                            ChromeSessions[chromeSession] = inUse;
                            break;
                        }
                    }

                    //Throw exception.. server is busy..
                    throw new NoAvailableInstacesException($"There is no free process to handle your request. Try again later, please.");

                    #endregion
                }

                ChromeSessions[chromeSession] = inUse;
            }

            return chromeSession;
        }

        /// <summary>
        /// Mark chrome session as 'NotInUse'
        /// </summary>
        public static void ReleaseInstance(ChromeSession chromeSession)
        {
            const bool notInUse = false;
            ChromeSessions[chromeSession] = notInUse;
        }

        public static void Dispose()
        {
            _chromeProcess?.Dispose();
        }
    }
}