using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CssOptimizer.Domain.Exceptions;

namespace CssOptimizer.Services.ChromeServices
{
    public static class ChromeSessionPool
    {
        #region Config

        private const int MinSessionPoolCount = 10;
        private const int MaxSessionPoolCount = 100;
        private const int TimeoutSeconds = 120;

        #endregion

        private static Chrome _chromeProcess;
        private static readonly ConcurrentDictionary<ChromeSession, bool> ChromeSessions = new ConcurrentDictionary<ChromeSession, bool>();

        public static async Task InitPool()
        {
            _chromeProcess = new Chrome();

            for (var i = 0; i < MinSessionPoolCount; i++)
            {
                var chromeSession = await _chromeProcess.CreateNewSession();
                ChromeSessions.TryAdd(chromeSession, false);
            }
        }

        public static Task<IEnumerable<ChromeSession>> GetActiveSessions()
        {
            return Task.FromResult(ChromeSessions.Keys.AsEnumerable());
        }

        public static ChromeSession GetInstance()
        {
            const bool inUse = true;
            ChromeSession chromeSession;

            lock (ChromeSessions)
            {
                chromeSession = ChromeSessions.FirstOrDefault(s => !s.Value).Key;

                if (chromeSession == null)
                {
                    //Create new instance if all are busy, and max session count is not reached 
                    if (ChromeSessions.Count < MaxSessionPoolCount)
                    {
                        chromeSession = _chromeProcess.CreateNewSession().Result;
                        ChromeSessions.TryAdd(chromeSession, false);
                        return chromeSession;
                    }

                    #region Timeout logic

                    var timeoutTime = DateTime.Now.AddSeconds(TimeoutSeconds);

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


        public static void ReleaseInstance(ChromeSession chromeSession)
        {
            const bool notInUse = false;
            ChromeSessions[chromeSession] = notInUse;
        }
    }
}