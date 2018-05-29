using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaristaLabs.ChromeDevTools.Runtime.Page;
using BaristaLabs.ChromeDevTools.Runtime.Runtime;
using CssOptimizer.Services.ChromeServices.Protocol;
using Newtonsoft.Json;
using Tera.ChromeDevTools;

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

        /// <summary>
        /// A handler that can carry the active session and the current timestamp
        /// </summary>
        /// <param name="session">The session that triggered the event</param>
        /// <param name="timestamp">The timestamp of this event</param>
        public delegate void PageLoadedEventHandler(ChromeSession session, double timestamp);

        /// <summary>
        /// Event that triggers when a page is loaded
        /// </summary>
        public event PageLoadedEventHandler PageLoaded;

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
            InternalSession.Page.SubscribeToLoadEventFiredEvent((evt) =>
            {
                //this should trigger when a page loads.
                PageLoaded?.Invoke(this, evt.Timestamp);
            });
        }

        /// <summary>
        /// Redirects the current session to the given url
        /// </summary>
        /// <param name="url">The desired url</param>
        /// <returns></returns>
        public async Task Navigate(string url)
        {
            await InternalSession.Page.Navigate(new NavigateCommand() { Url = url });
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