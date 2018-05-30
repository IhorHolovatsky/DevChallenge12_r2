using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CssOptimizer.Services.ChromeServices.Protocol;
using CssOptimizer.Services.Utils;
using Newtonsoft.Json;

namespace CssOptimizer.Services.ChromeServices
{
    /// <summary>
    /// Represents a Chrome instance.
    /// Might not work if you try to open several instances on the same port.
    /// </summary>
    public class Chrome : IDisposable
    {
        private Process _chromeProcess;
        private readonly int _remoteDebuggingPort;
        private readonly Dictionary<string, ChromeSession> _aliveSessions;

        /// <summary>
        /// Initializes an instance of Chrome
        /// </summary>
        /// <param name="remoteDebuggingPort">The port provided for the remote debugger</param>
        /// <param name="headless">indicates if the window should be hidden</param>
        public Chrome(int remoteDebuggingPort = 9222, bool headless = true)
        {
            var directoryInfo = ChromeUtils.CreateTempFolder();
            _remoteDebuggingPort = remoteDebuggingPort;
            var remoteDebuggingArg = $"--remote-debugging-port={remoteDebuggingPort}";
            var userDirectoryArg = $"--user-data-dir=\"{directoryInfo}\"";
            var chromeProcessArgs = $"{remoteDebuggingArg} {userDirectoryArg} --bwsi --no-first-run";

            if (headless)
            {
                chromeProcessArgs += " --headless";
            }

            _chromeProcess = Process.Start(ChromeUtils.GetChromePath(), chromeProcessArgs);
            System.Threading.Thread.Sleep(100);
            _aliveSessions = new Dictionary<string, ChromeSession>();
        }


        /// <summary>
        /// Ennumerates the available sessions(tabs) 
        /// </summary>
        /// <returns>A collection of ChromeSessions</returns>
        public async Task<IEnumerable<ChromeSession>> GetActiveSessions()
        {
            using (var webClient = GetDebuggerClient())
            {
                var remoteSessions = await webClient.GetStringAsync("/json");
                var validSessions = new List<string>();
                foreach (var item in JsonConvert.DeserializeObject<ICollection<ChromeSessionInfo>>(remoteSessions)
                    .Where(s => s.Type == "page")
                    )
                {
                    validSessions.Add(item.Id);
                    if (!_aliveSessions.ContainsKey(item.Id))
                    {
                        AddSession(item);
                    }

                }
                foreach (var invalidKey in _aliveSessions.Keys.Except(validSessions))
                {
                    _aliveSessions[invalidKey].Dispose();
                }

                return _aliveSessions.Values.ToArray();
            }
        }

        /// <summary>
        /// Creates and returns a new Session (Tab)
        /// </summary>
        /// <returns>The newly created Session</returns>
        public async Task<ChromeSession> CreateNewSession()
        {
            using (var webClient = GetDebuggerClient())
            {
                var result = await webClient.PostAsync("/json/new", null);
                var contents = await result.Content.ReadAsStringAsync();
                return AddSession(JsonConvert.DeserializeObject<ChromeSessionInfo>(contents));
            }
        }

        /// <summary>
        /// Closes the provided Session(Tab)
        /// </summary>
        /// <param name="session">Session to be closed</param>
        /// <returns></returns>
        public async Task CloseSession(ChromeSession session)
        {
            //TODO:if i close all the sessions, chrome closes itself! i can use this to gracefully close this stuff, OR maybe i should prevent it?
            using (var webClient = GetDebuggerClient())
            {
                var result = await webClient.PostAsync("/json/close/" + session.Id, null);
                var contents = await result.Content.ReadAsStringAsync();
                //Assert contents == "Target is closing" 
                if (_aliveSessions.ContainsKey(session.Id))
                    _aliveSessions.Remove(session.Id);
                session.InnerDispose();
            }
        }

        private ChromeSession AddSession(ChromeSessionInfo info)
        {
            var session = new ChromeSession(info, this);
            _aliveSessions.Add(session.Id, session);
            return session;
        }

        private HttpClient GetDebuggerClient()
        {
            var chromeHttpClient = new HttpClient
            {
                BaseAddress = new Uri($"http://localhost:{_remoteDebuggingPort}")
            };

            return chromeHttpClient;
        }
        
        #region Closing & cleaning

        private void CloseProcess()
        {
            if (_chromeProcess != null)
            {
                _chromeProcess.CloseMainWindow();
                if (!_chromeProcess.WaitForExit(1000))
                {
                    _chromeProcess.Kill();
                }
                _chromeProcess.Dispose();
                _chromeProcess = null;
            }
        }

        private void CloseSessions()
        {
            foreach (var item in _aliveSessions.Values.ToArray())
            {
                CloseSession(item).GetAwaiter().GetResult();
            }

        }

        ~Chrome()
        {
            CloseSessions();
            CloseProcess();
        }

        /// <summary>
        /// Closes the active sessions & finalizes the process created by this instance
        /// </summary>
        public void Dispose()
        {
            CloseSessions();
            CloseProcess();
        }

        #endregion
    }
}