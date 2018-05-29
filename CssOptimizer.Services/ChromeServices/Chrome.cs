using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CssOptimizer.Services.Utils;
using MasterDevs.ChromeDevTools;
using Newtonsoft.Json;

namespace CssOptimizer.Services.ChromeServices
{
    internal class Chrome
    {
        private Process _chromeProcess;

        private readonly int _remoteDebuggingPort;
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
            Thread.Sleep(100);
        }

        /// <summary>
        /// Creates and returns a new Session (Tab)
        /// </summary>
        /// <returns>The newly created Session</returns>
        internal async Task<ChromeSessionInfo> CreateNewSessionAsync()
        {
            using (var webClient = GetDebuggerClient())
            {
                var result = await webClient.PostAsync("/json/new", null);
                var contents = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ChromeSessionInfo>(contents);
            }
        }

        private HttpClient GetDebuggerClient()
        {
            var chromeHttpClient = new HttpClient
            {
                BaseAddress = new Uri($"http://localhost:{_remoteDebuggingPort}")
            };

            return chromeHttpClient;
        }
    }
}