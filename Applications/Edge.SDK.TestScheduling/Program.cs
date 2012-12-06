using Edge.UnitTests.Core.Services.Scheduling;
namespace Edge.SDK.TestScheduling
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new SchedulerTest();
            test.TestWorkflowServices();
        }
    }
}
