using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CssOptimizer.Domain.Exceptions;
using MasterDevs.ChromeDevTools;

namespace CssOptimizer.Services.ChromeServices
{
    public static class ChromeSessionPool
    {
        #region Config

        private const int MIN_SESSION_POOL_COUNT = 10;
        private const int MAX_SESSION_POOL_COUNT = 100;
        private const int TIMEOUT_SECONDS = 120;

        #endregion

        private static ChromeSessionInfo _chromeSessionInfo;
        private static readonly ConcurrentDictionary<IChromeSession, bool> _chromeSessions = new ConcurrentDictionary<IChromeSession, bool>();

        public static void InitPool()
        {
            var initTasks = new List<Task>();

            var sessionInfo = new Chrome().CreateNewSessionAsync().Result;
            _chromeSessionInfo = sessionInfo ?? throw new Exception("Chrome was not able to start!");

            var chromeSessionFactory = new ChromeSessionFactory();

            for (var i = 0; i < MIN_SESSION_POOL_COUNT; i++)
            {
                initTasks.Add(Task.Factory.StartNew(() =>
                {
                    var chromeSession = chromeSessionFactory.Create(sessionInfo.WebSocketDebuggerUrl);
                    _chromeSessions.TryAdd(chromeSession, false);
                }));
            }

            Task.WaitAll(initTasks.ToArray());
        }

        public static IChromeSession GetInstance()
        {
            const bool IN_USE = true;
            var chromeSession = _chromeSessions.FirstOrDefault(s => !s.Value).Key;

            if (chromeSession == null)
            {
                //Create new instance if all are busy, and max session count is not reached 
                if (_chromeSessions.Count < MAX_SESSION_POOL_COUNT)
                {
                    var chromeSessionFactory = new ChromeSessionFactory();
                    chromeSession = chromeSessionFactory.Create(_chromeSessionInfo.WebSocketDebuggerUrl);
                    _chromeSessions.TryAdd(chromeSession, IN_USE);
                    return chromeSession;
                }

                #region Timeout logic

                var timeoutTime = DateTime.Now.AddSeconds(TIMEOUT_SECONDS);

                //Wait until timeout
                while (DateTime.Now < timeoutTime)
                {
                    Thread.Sleep(500);

                    chromeSession = _chromeSessions.FirstOrDefault(s => !s.Value).Key;

                    if (chromeSession != null)
                    {
                        _chromeSessions[chromeSession] = IN_USE;
                        break;
                    }
                }

                //Throw exception.. server is busy..
                throw new NoAvailableInstacesException($"There is no free process to handle your request. Try again later, please.");

                #endregion
            }

            _chromeSessions[chromeSession] = IN_USE;

            return chromeSession;
        }


        public static void ReleaseInstance(IChromeSession chromeSession)
        {
            const bool NOT_IN_USE = false;
            _chromeSessions[chromeSession] = NOT_IN_USE;
        }
    }
}