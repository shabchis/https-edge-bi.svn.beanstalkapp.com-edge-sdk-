using System;
using Edge.Core.Utilities;
using Edge.UnitTests.Core.Services.Scheduling;

namespace Edge.SDK.TestScheduling
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new SchedulerTest();

			log4net.Config.XmlConfigurator.Configure();
			Log.Start();

			Log.Write("TestScheduling", "Starting Scheduling Test", LogMessageType.Debug);
			
            //--------------------------------
            // Workflow Test
            //-------------------------------
            //test.TestWorkflowServices(false);

            //--------------------------------
            // Full Instegration Test
            //-------------------------------
			//test.TestFullIntegration(false);

			//--------------------------------
			// Stress Test
			//-------------------------------
			//test.StressTest(false);

			//--------------------------------
			// Google Adwords Test
			//-------------------------------
			test.TestGoogleAdwords(false);

			Log.Write("TestScheduling", "Started Scheduling Test", LogMessageType.Debug);

			do
			{

			} while (Console.ReadLine() != "exit");
        }
    }
}
