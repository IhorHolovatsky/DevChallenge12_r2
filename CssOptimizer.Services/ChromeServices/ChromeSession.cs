using System;
using System.Threading.Tasks;
using CssOptimizer.Services.ChromeServices.Protocol;
using Newtonsoft.Json;

namespace CssOptimizer.Services.ChromeServices
{
    /// <summary>
    /// Represents a single Chrome Session (Tab)
    /// </summary>
    public class ChromeSession : IDisposable
    {
        /// <summary>
        /// The unique Id of this Session
        /// </summary>
        public string Id { get; }

        [JsonIgnore]
        public BaristaLabs.ChromeDevTools.Runtime.ChromeSession InternalSession;
        public ChromeSessionInfo InternalNativeSession { get; set; }

        public int CommandTimeout => InternalSession.CommandTimeout;

        private readonly Chrome _chrome;

        internal ChromeSession(ChromeSessionInfo chromeSessionInfo, Chrome chrome, int commandTimeout = 120)
        {
            Id = chromeSessionInfo.Id;
            _chrome = chrome;
            InternalSession = new BaristaLabs.ChromeDevTools.Runtime.ChromeSession(chromeSessionInfo.WebSocketDebuggerUrl)
            {
                //TODO: move to config
                CommandTimeout = commandTimeout * 1000
            };
            InternalNativeSession = chromeSessionInfo;

            Task.WaitAll(InitializePage());
        }

        private async Task InitializePage()
        {
            //enables events.
            await InternalSession.Page.Enable();
            await Task.Delay(100);
        }

        #region cleanup

        ~ChromeSession()
        {
            InternalSession?.Dispose();
        }

        internal void InnerDispose()
        {
            InternalSession.Dispose();
            InternalSession = null;

        }

        public void Dispose()
        {
            _chrome.CloseSession(this).GetAwaiter().GetResult();
        }

        #endregion
    }
}