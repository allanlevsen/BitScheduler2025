using BitTimeScheduler.TestsPerformanceTesting;

namespace BitTimeTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var test1 = new BitDayMonthTest();
            test1.RunBitDayCorrectnessTests();
            test1.RunBitDayPerformanceTests();

            // Run the validity tests.
            test1.RunEmptyWeekdayAvailabilityTest();
            test1.RunMultipleWeekdayAvailabilityTest();

            // Run the performance tests.
            test1.RunEmptyWeekdayAvailabilityPerformanceTest();
            test1.RunMultipleWeekdayAvailabilityPerformanceTest();


            var test2 = new BitDayUtilityTests();
            test2.RunUtilityMethodsTests();
            test2.RunUtilityMethodsPerformanceTests();

            var test3 = new BitScheduleTests();
            test3.RunFunctionalTests();
            test3.RunPerformanceTests();
            test3.TestConfigurationChangeRefresh();

            //Console.ReadLine();

        }
    }
}
