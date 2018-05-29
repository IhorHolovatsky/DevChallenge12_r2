namespace CssOptimizer.Domain.Validation
{
    public class ResponseWrapper<T> where T : class
    {
        public ResponseErrors ValidationErrors { get; set; }
        public bool IsSuccess => ValidationErrors == null || ValidationErrors.Count == 0;
        public T Items { get; set; }
    }
}