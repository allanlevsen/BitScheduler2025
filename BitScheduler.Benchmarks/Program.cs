using BenchmarkDotNet.Running;

namespace BitScheduler.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<BitDayReserveFreeBenchmarks>();
    }
}
