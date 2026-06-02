using BitTimeScheduler.Services;

namespace BitTimeTestApp
{
    internal static class Program
    {
        static async Task Main()
        {
            var runner = new PerformanceTestRunner();
            await runner.RunAsync();
        }
    }
}
