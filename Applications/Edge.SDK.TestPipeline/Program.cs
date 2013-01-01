using System;
using Edge.Core;
using Edge.Core.Services;
using Edge.Core.Services.Workflow;
using Edge.Data.Pipeline.Metrics.Services;
using Edge.Data.Pipeline.Metrics.Services.Configuration;
using Edge.Data.Pipeline.Services;

namespace Edge.SDK.TestPipeline
{
	class Program
	{
		public enum ProcessorType
		{
			Generic,
			Add
		}

		static void Main()
		{
			// change processor type to test Generic or Add processor types
			const ProcessorType testProccessorType = ProcessorType.Generic;

			var environment = CreateEnvironment();

			// wprkflow configuration
			var workflowConfig = CreatePipelineWorkflowConfiguration(testProccessorType);
			var profile = new ServiceProfile { Name = "PipelineProfile" };
			profile.Parameters["AccountID"] = 1;
			var profileServiceConfig = profile.DeriveConfiguration(workflowConfig);

			using (new ServiceExecutionHost(environment.EnvironmentConfiguration.DefaultHostName, environment))
			{
				using (var listener = environment.ListenForEvents(ServiceEnvironmentEventType.ServiceRequiresScheduling))
				{
					listener.ServiceRequiresScheduling += Environment_ServiceRequiresScheduling;

					do
					{
						// create and start service
						var instance = environment.NewServiceInstance(profileServiceConfig);
						instance.StateChanged += instance_StateChanged;
						instance.OutputGenerated += instance_OutputGenerated;
						instance.Start();
					} while (Console.ReadKey().Key != ConsoleKey.Escape);
				}
			}
		}

		#region Configuration
		private static ServiceConfiguration CreatePipelineWorkflowConfiguration(ProcessorType type)
		{
			var workflowConfig = new WorkflowServiceConfiguration
				{
					ServiceName = "PipelineWorkflow",
					Workflow = new WorkflowNodeGroup
						{
							Mode = WorkflowNodeGroupMode.Linear,
							Nodes = new LockableList<WorkflowNode>
								{
									new WorkflowStep {Name = "PipelileTestInitializer", ServiceConfiguration = GetInitializerConfig()},
									new WorkflowStep {Name = "PipelileTestRetriever", ServiceConfiguration = GetRetrieverConfig()},
									new WorkflowStep {Name = "PipelileTestProcessor", ServiceConfiguration = type == ProcessorType.Generic ? GetGenericProcessorConfig() : GetAdProcessorConfig()},
								}
						}
				};

			return workflowConfig;
		}

		private static ServiceConfiguration GetInitializerConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(UrlInitializerService).AssemblyQualifiedName
			};

			return config;
		}

		private static ServiceConfiguration GetRetrieverConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(UrlRetrieverService).AssemblyQualifiedName
			};

			return config;
		}

		private static ServiceConfiguration GetGenericProcessorConfig()
		{
			var config = new AutoMetricsProcessorServiceConfiguration
			{
				ServiceClass = typeof(AutoGenericMetricsProcessorService).AssemblyQualifiedName
			};

			return config;
		}

		private static ServiceConfiguration GetAdProcessorConfig()
		{
			var config = new AutoMetricsProcessorServiceConfiguration
			{
				ServiceClass = typeof(AutoAdMetricsProcessorService).AssemblyQualifiedName
			};

			return config;
		}

		private static ServiceEnvironment CreateEnvironment()
		{
			// create service env
			var envConfig = new ServiceEnvironmentConfiguration
			{
				DefaultHostName = "Shira",
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

			var environment = ServiceEnvironment.Open("Pipeline Test", envConfig);

			return environment;
		} 
		#endregion

		#region Events
		private static void Environment_ServiceRequiresScheduling(object sender, ServiceInstanceEventArgs e)
		{
			Console.WriteLine("     --> child of: {0}", e.ServiceInstance.ParentInstance != null ? e.ServiceInstance.ParentInstance.Configuration.ServiceName : "(no parent)");
			e.ServiceInstance.StateChanged += instance_StateChanged;
			e.ServiceInstance.OutputGenerated += instance_OutputGenerated;
			e.ServiceInstance.Start();
		}

		static void instance_StateChanged(object sender, EventArgs e)
		{
			var instance = (ServiceInstance)sender;
			Console.WriteLine("{3} ({4}) -- state: {0}, progress: {1}, outcome: {2}", instance.State, instance.Progress, instance.Outcome, instance.Configuration.ServiceName, instance.InstanceID.ToString("N").Substring(0, 4));
		}


		static void instance_OutputGenerated(object sender, ServiceOutputEventArgs e)
		{
			Console.WriteLine("     --> " + e.Output);
		} 
		#endregion
	}
}
