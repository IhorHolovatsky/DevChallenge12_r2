using System.Collections.Generic;

namespace CssOptimizer.Domain.Validation
{
    public class ResponseErrors : List<ResponseError>
    {
        public void Add(string code, string message)
        {
            Add(new ResponseError(code, message));
        }

        public bool IsEmpty => Count == 0;
    }
}