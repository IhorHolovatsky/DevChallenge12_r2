namespace CssOptimizer.Domain.Validation
{
    public class ResponseError
    {
        public string Code { get; }

        public string Message { get; }

        public ResponseError(string code, string message)
        {
            Code = code;
            Message = message;
        }
    }
}