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
    public class CustomOptimizeTests : IClassFixture<TestServerFixture>
    {
        private readonly HttpClient _client;

        public CustomOptimizeTests(TestServerFixture testServerFixture)
        {
            _client = testServerFixture?.Client;
        }

        [Theory, Priority(0)]
        [InlineData("https://google.com/")]
        [InlineData("https://github.com/AngleSharp/AngleSharp/blob/master/src/AngleSharp/Dom/Css/Selector/UnknownSelector.cs")]
        [InlineData("https://github.com/AngleSharp/AngleSharp/issues/550")]
        [InlineData("https://www.privat24.ua/")]
        [InlineData("https://play.google.com/music/listen?hl=en#/artist/Ae6rhr3mtuuechm5jpkev4vqulm/Ibenji")]
        [InlineData("https://mail.google.com/mail/u/0/#search/has%3Anouserlabels")]
        public async Task CustomOptimizeUrl(string url)
        {
            // Act
            var response = await _client.GetAsync("/api/v2/optimize/css?url=" + url);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            
            // Assert
            Assert.True(!string.IsNullOrEmpty(responseString));
        }

        [Theory, Priority(1)]
        [ClassData(typeof(ValidUrlsTestData))]
        public async Task CustomOptimizeUrlInParallel(List<string> urls)
        {
            // Act
            var response = await _client.PostAsync("/api/v2/optimize/css/parallel", new StringContent(JsonConvert.SerializeObject(urls), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(!string.IsNullOrEmpty(responseString));
        }

        [Theory, Priority(2)]
        [ClassData(typeof(InvalidUrlsTestData))]
        public async Task CustomOptimizeInvalidUrl(List<string> urls)
        {
            // Act
            var response = await _client.PostAsync("/api/v2/optimize/css/parallel", new StringContent(JsonConvert.SerializeObject(urls), Encoding.UTF8, "application/json"));
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<ResponseErrors>(responseString);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest);
            Assert.True(!string.IsNullOrEmpty(responseString));
            Assert.True(responseObject?.Count > 0);
        }
    }
}
