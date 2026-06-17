using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BitSchedulerCore;

namespace BitScheduler.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BitDayReserveFreeBenchmarks
{
    private readonly DateTime _benchmarkDate = new(2025, 1, 1);

    [Params(10, 100, 1_000, 10_000, 100_000)]
    public int OperationCount { get; set; }

    [Params(1, 2, 4)]
    public int SlotLength { get; set; }

    [Benchmark(Baseline = true)]
    public int ReserveAndFreeSequentialSingleBitDay()
    {
        var day = new BitDay(_benchmarkDate);
        var successfulReservations = 0;

        for (var i = 0; i < OperationCount; i++)
        {
            var startSlot = GetSequentialStartSlot(i, SlotLength);

            if (day.ReserveRange(startSlot, SlotLength))
            {
                successfulReservations++;
            }

            day.FreeRange(startSlot, SlotLength);
        }

        return successfulReservations;
    }

    [Benchmark]
    public int ReserveAndFreeAlternatingSingleBitDay()
    {
        var day = new BitDay(_benchmarkDate);
        var successfulReservations = 0;

        for (var i = 0; i < OperationCount; i++)
        {
            var startSlot = GetAlternatingStartSlot(i, SlotLength);

            if (day.ReserveRange(startSlot, SlotLength))
            {
                successfulReservations++;
            }

            day.FreeRange(startSlot, SlotLength);
        }

        return successfulReservations;
    }

    [Benchmark]
    public int ReserveThenFreeBatchedSingleBitDay()
    {
        var day = new BitDay(_benchmarkDate);
        var starts = new int[BitDay.TotalSlots / SlotLength];
        var successfulReservations = 0;

        for (var i = 0; i < starts.Length; i++)
        {
            starts[i] = i * SlotLength;
        }

        for (var iteration = 0; iteration < OperationCount; iteration++)
        {
            var startSlot = starts[iteration % starts.Length];
            if (day.ReserveRange(startSlot, SlotLength))
            {
                successfulReservations++;
            }
        }

        for (var iteration = 0; iteration < OperationCount; iteration++)
        {
            var startSlot = starts[iteration % starts.Length];
            day.FreeRange(startSlot, SlotLength);
        }

        return successfulReservations;
    }

    [Benchmark]
    public int ReserveAndFreeAcrossRollingDays()
    {
        var days = CreateDays(32);
        var successfulReservations = 0;

        for (var i = 0; i < OperationCount; i++)
        {
            var day = days[i % days.Length];
            var startSlot = GetSequentialStartSlot(i, SlotLength);

            if (day.ReserveRange(startSlot, SlotLength))
            {
                successfulReservations++;
            }

            day.FreeRange(startSlot, SlotLength);
        }

        return successfulReservations;
    }

    private static int GetSequentialStartSlot(int iteration, int slotLength)
    {
        var maxStart = BitDay.TotalSlots - slotLength;
        return (iteration * slotLength) % (maxStart + 1);
    }

    private static int GetAlternatingStartSlot(int iteration, int slotLength)
    {
        var maxStart = BitDay.TotalSlots - slotLength;
        var reverseIndex = maxStart - ((iteration * slotLength) % (maxStart + 1));
        return Math.Max(0, reverseIndex);
    }

    private BitDay[] CreateDays(int count)
    {
        var days = new BitDay[count];
        for (var i = 0; i < count; i++)
        {
            days[i] = new BitDay(_benchmarkDate.AddDays(i));
        }

        return days;
    }
}
