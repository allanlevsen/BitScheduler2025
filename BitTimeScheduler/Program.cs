using BitTimeScheduler.Services;

namespace BitTimeScheduler;

internal static class Program
{
    static async Task Main()
    {
        var runner = new PerformanceTestRunner();
        await runner.RunAsync();
    }
}
