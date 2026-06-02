using BitTimeScheduler.TestsPerformanceTesting;

namespace BitTimeScheduler.Services;

internal sealed class PerformanceTestRunner
{
    public async Task RunAsync()
    {
        var dayMonthTests = new BitDayMonthTest();
        dayMonthTests.RunBitDayCorrectnessTests();
        dayMonthTests.RunBitDayPerformanceTests();
        dayMonthTests.RunEmptyWeekdayAvailabilityTest();
        dayMonthTests.RunMultipleWeekdayAvailabilityTest();
        dayMonthTests.RunEmptyWeekdayAvailabilityPerformanceTest();
        dayMonthTests.RunMultipleWeekdayAvailabilityPerformanceTest();

        var utilityTests = new BitDayUtilityTests();
        utilityTests.RunUtilityMethodsTests();
        utilityTests.RunUtilityMethodsPerformanceTests();
        utilityTests.TestCreateRangeFromBlocks();

        var scheduleTests = new BitScheduleTests();
        await scheduleTests.RunFunctionalTests();
        await scheduleTests.RunPerformanceTests();
        scheduleTests.TestConfigurationChangeRefresh();
    }
}
