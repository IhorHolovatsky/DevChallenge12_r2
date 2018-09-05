using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CssOptimizer.Domain.Validation;
using CssOptimizer.Tests.Attributes;
using CssOptimizer.Tests.Fixtures;
using CssOptimizer.Tests.TestCases;
using Newtonsoft.Json;
using Xunit;

namespace CssOptimizer.Tests
{
    [TestCaseOrderer("CssOptimizer.Tests.Attributes.PriorityOrderer", "CssOptimizer.Tests")]
    public class BrowserOptimizeTests : IClassFixture<TestServerFixture>
    {
        private readonly HttpClient _client;

        public BrowserOptimizeTests(TestServerFixture testServerFixture)
        {
            _client = testServerFixture?.Client;
        }

        [Theory(Skip = "Require more work to properly dispose chrome session pool"), Priority(201)]
        [InlineData("https://google.com/")]
        [InlineData("https://github.com/AngleSharp/AngleSharp/blob/master/src/AngleSharp/Dom/Css/Selector/UnknownSelector.cs")]
        [InlineData("https://github.com/AngleSharp/AngleSharp/issues/550")]
        [InlineData("https://www.privat24.ua/")]
        [InlineData("https://mail.google.com/mail/u/0/#search/has%3Anouserlabels")]
        public async Task BrowserOptimizeUrl(string url)
        {
            // Act
            var response = await _client.GetAsync("/api/v1/optimize/css?url=" + url);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(!string.IsNullOrEmpty(responseString));
        }

        [Theory(Skip = "Require more work to properly dispose chrome session pool"), Priority(202)]
        [ClassData(typeof(ValidUrlsTestData))]
        public async Task BrowserOptimizeUrlInParallel(List<string> urls)
        {
            // Act
            var response = await _client.PostAsync("/api/v1/optimize/css/parallel", new StringContent(JsonConvert.SerializeObject(urls), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(!string.IsNullOrEmpty(responseString));
        }

        [Theory(Skip = "Require more work to properly dispose chrome session pool"), Priority(203)]
        [ClassData(typeof(InvalidUrlsTestData))]
        public async Task BrowserOptimizeInvalidUrl(List<string> urls)
        {
            // Act
            var response = await _client.PostAsync("/api/v1/optimize/css/parallel", new StringContent(JsonConvert.SerializeObject(urls), Encoding.UTF8, "application/json"));
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<ResponseErrors>(responseString);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest);
            Assert.True(!string.IsNullOrEmpty(responseString));
            Assert.True(responseObject?.Count > 0);
        }
    }
}