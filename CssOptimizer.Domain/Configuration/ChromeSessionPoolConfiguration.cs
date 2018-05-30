namespace CssOptimizer.Domain.Configuration
{
    public class ChromeSessionPoolConfiguration
    {
        public int ChromeDebuggingPort { get; set; }
        public bool IsHeadlessMode { get; set; }
        public int MaxSessionPoolCount { get; set; }
        public int CommandTimeout { get; set; }
        public int RequestTimeout { get; set; }
    }
}