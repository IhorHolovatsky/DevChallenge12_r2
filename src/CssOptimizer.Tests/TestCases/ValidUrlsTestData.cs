using System.Collections;
using System.Collections.Generic;

namespace CssOptimizer.Tests.TestCases
{
    public class ValidUrlsTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new List<string> { "https://google.com/", "https://github.com/AngleSharp/AngleSharp/blob/master/src/AngleSharp/Dom/Css/Selector/UnknownSelector.cs" } };
            yield return new object[] { new List<string> { "https://www.youtube.com/", "https://outlook.live.com/mail/?authRedirect=true", "https://google.com/" } };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}