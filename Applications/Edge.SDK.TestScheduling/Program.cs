using System;
using Edge.UnitTests.Core.Services.Scheduling;
namespace Edge.SDK.TestScheduling
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new SchedulerTest();
            
            //--------------------------------
            // Workflow Test
            //-------------------------------
            test.TestWorkflowServices();

            //--------------------------------
            // Full Instegration Test
            //-------------------------------
			//test.TestFullIntegration();
			//do
			//{
                
			//} while (Console.ReadLine() != "exit");
        }
    }
}
