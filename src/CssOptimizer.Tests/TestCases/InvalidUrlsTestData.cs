using System.Collections;
using System.Collections.Generic;

namespace CssOptimizer.Tests.TestCases
{
    public class InvalidUrlsTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new List<string> { "sdf", "htts://tor.cs" } };
            yield return new object[] { new List<string> { "youtube.com", "", null } };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}