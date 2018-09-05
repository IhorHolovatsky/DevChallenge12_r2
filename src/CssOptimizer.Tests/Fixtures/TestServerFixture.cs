using System;
using System.Collections.Generic;
using System.Net.Http;
using CssOptimizer.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace CssOptimizer.Tests.Fixtures
{
    public class TestServerFixture : IDisposable
    {
        public TestServer Server { get; set; }
        public HttpClient Client { get; set; }

        public TestServerFixture()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                {"ChromeSessionPool:ChromeDebuggingPort", "9222" },
                {"ChromeSessionPool:IsHeadlessMode", "true" },
                //It's require to properly dispose chrome processes after unit test, let's just skip it... it require more work
                {"ChromeSessionPool:IsPreInitializeChromeSessionPool", "false" },
                {"ChromeSessionPool:WaitForInitializing", "true" },
                {"ChromeSessionPool:MaxSessionPoolCount", "2" },
                {"ChromeSessionPool:CommandTimeout", "120" },
                {"ChromeSessionPool:RequestTimeout", "120" },
                {"Cache:UrlCacheTime", "3600" }
            };

            Server = new TestServer(new WebHostBuilder()
                                        .UseStartup<Startup>()
                                        .ConfigureAppConfiguration(context => context.AddInMemoryCollection(config)));

            Client = Server.CreateClient();
        }

        public void Dispose()
        {
            Client.Dispose();
            Server.Dispose();
        }
    }
}