using System.Net.Http;
using System.Threading.Tasks;
using CssOptimizer.Tests.Attributes;
using CssOptimizer.Tests.Fixtures;
using Xunit;

namespace CssOptimizer.Tests
{
    [TestCaseOrderer("CssOptimizer.Tests.Attributes.PriorityOrderer", "CssOptimizer.Tests")]
    public class CacheResetTests : IClassFixture<TestServerFixture>
    {
        private readonly HttpClient _client;

        public CacheResetTests(TestServerFixture testServerFixture)
        {
            _client = testServerFixture?.Client;
        }

        [Theory, Priority(101)]
        [InlineData("https://google.com/")]
        [InlineData("https://github.com/AngleSharp/AngleSharp/blob/master/src/AngleSharp/Dom/Css/Selector/UnknownSelector.cs")]
        [InlineData("https://github.com/AngleSharp/AngleSharp/issues/550")]
        [InlineData("https://www.privat24.ua/")]
        [InlineData("https://play.google.com/music/listen?hl=en#/artist/Ae6rhr3mtuuechm5jpkev4vqulm/Ibenji")]
        [InlineData("https://mail.google.com/mail/u/0/#search/has%3Anouserlabels")]
        public async Task CacheResetForUrlTest(string url)
        {
            // Act
            var response = await _client.DeleteAsync("/api/cache/reset/url?url=" + url);
            response.EnsureSuccessStatusCode();
        }

        [Fact, Priority(102)]
        public async Task CacheResetAllTest()
        {
            // Act
            var response = await _client.DeleteAsync("/api/cache/reset");
            response.EnsureSuccessStatusCode();
        }

    }
}