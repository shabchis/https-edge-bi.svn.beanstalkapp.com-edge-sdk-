using System;
using Edge.Core.Services;
using Edge.Core.Utilities;

namespace Edge.SDK.TestEnvironment
{
	class Program
	{
		static void Main()
		{
			log4net.Config.XmlConfigurator.Configure();
			Log.Start();

			// Create Environment and host
			var envConfig = new ServiceEnvironmentConfiguration
			{
				DefaultHostName = "Johnny",
				ConnectionString = "Data Source=bi_rnd;Initial Catalog=EdgeSystem;Integrated Security=true",
				SP_HostListGet = "Service_HostList",
				SP_HostRegister = "Service_HostRegister",
				SP_HostUnregister = "Service_HostUnregister",
				SP_InstanceSave = "Service_InstanceSave",
				SP_InstanceGet = "Service_InstanceGet",
				SP_InstanceReset = "Service_InstanceReset",
				SP_EnvironmentEventListenerListGet = "Service_EnvironmentEventListenerListGet",
				SP_EnvironmentEventListenerRegister = "Service_EnvironmentEventListenerRegister",
				SP_EnvironmentEventListenerUnregister = "Service_EnvironmentEventListenerUnregister",
				SP_ServicesExecutionStatistics = "Service_ExecutionStatistics_GetByPercentile",
				SP_InstanceActiveListGet = "Service_InstanceActiveList_GetByTime"
			};

			var environment = ServiceEnvironment.Open(envConfig);
			var host = new ServiceExecutionHost(environment.EnvironmentConfiguration.DefaultHostName, environment);

			Log.Write("TestEnvironment", "Started Environment", LogMessageType.Debug);

			do
			{

			} while (Console.ReadLine() != "exit");
		}
	}
}
