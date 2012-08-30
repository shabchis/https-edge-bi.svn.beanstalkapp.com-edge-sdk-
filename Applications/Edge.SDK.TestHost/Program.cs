﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Edge.Core;
using System.Threading;
using Edge.Core.Services;
using Edge.Core.Services.Workflow;

namespace Edge.SDK.TestHost
{
	class Program
	{
		static void Main(string[] args)
		{
			// ..........................................................
			// STEP 1 - host

			var envConfig = new ServiceEnvironmentConfiguration()
			{
				DefaultHostName = "Johnny",
				ConnectionString = "Data Source=bi_rnd;Initial Catalog=EdgeSystem;Integrated Security=true",
				SP_HostList = "Service_HostList",
				SP_HostRegister = "Service_HostRegister",
				SP_HostUnregister = "Service_HostUnregister",
				SP_InstanceSave = "Service_InstanceSave",

			};

			var environment = new ServiceEnvironment(envConfig);
			var host = new ServiceExecutionHost(environment.EnvironmentConfiguration.DefaultHostName, environment);

			//host.Environment.ServiceScheduleRequested += new EventHandler<ServiceInstanceEventArgs>(Environment_ServiceScheduleRequested);	

			// ..........................................................
			// STEP 2 - service

			var serviceTemplate = new ServiceConfiguration()
			{
				IsEnabled = true,
				ServiceName = "Test",
				ServiceClass = typeof(TestService).AssemblyQualifiedName,
				HostName = "Johnny"
			};


			#region workflow
			
			// ..........................................................
			// workflow example

			var stepConfig = new ServiceConfiguration()
			{
				ServiceClass = typeof(WorkflowStepExample).AssemblyQualifiedName
			};

			var workflowConfig = new WorkflowServiceConfiguration() { ServiceName = "PipelineExample" };
			workflowConfig.Workflow = new WorkflowNodeGroup()
			{
				Mode = WorkflowNodeGroupMode.Linear,
				Nodes = new LockableList<WorkflowNode>()
				{
					new WorkflowStep() { Name = "Initialize", ServiceConfiguration =  stepConfig},
					new WorkflowStep() { Name = "Retriever", ServiceConfiguration =  stepConfig},
					new WorkflowStep() { Name = "Processor", ServiceConfiguration =  stepConfig},
					new WorkflowStep() { Name = "Transform", ServiceConfiguration =  stepConfig},
					new WorkflowStep() { Name = "Stage", ServiceConfiguration =  stepConfig},
					new WorkflowStep() { Name = "Commit", ServiceConfiguration =  stepConfig},
				}
			};
			
			#endregion

			// ..........................................................
			var profile = new ServiceProfile()
			{
				Name = "Doron"
			};
			profile.Parameters["AccountID"] = 10035;

			ServiceConfiguration profileService = profile.DeriveConfiguration(workflowConfig);

			do
			{
				ServiceInstance instance = environment.NewServiceInstance(profileService);
				instance.StateChanged += new EventHandler(instance_StateChanged);
				instance.OutputGenerated += new EventHandler<ServiceOutputEventArgs>(instance_OutputGenerated);
				instance.Start();
			} while (Console.ReadLine() != "exit");
		}

		static void Environment_ServiceScheduleRequested(object sender, ServiceInstanceEventArgs e)
		{
			e.ServiceInstance.Start();
		}

		static void instance_StateChanged(object sender, EventArgs e)
		{
			var instance = (ServiceInstance) sender;
			Console.WriteLine("{3} ({4}) -- state: {0}, progress: {1}, outcome: {2}", instance.State, instance.Progress, instance.Outcome, instance.Configuration.ServiceName, instance.InstanceID.ToString("N").Substring(0,4));
		}


		static void instance_OutputGenerated(object sender, ServiceOutputEventArgs e)
		{
			Console.WriteLine("     --> " + e.Output.ToString());
		}

	}

	public class WorkflowStepExample : Service
	{
		protected override ServiceOutcome DoWork()
		{
			Thread.Sleep(TimeSpan.FromSeconds(2));
			return ServiceOutcome.Success;
		}
	}

	public class TestService : Service
	{
		protected override ServiceOutcome DoWork()
		{
			for (int i = 1; i < 10; i++)
			{
				Thread.Sleep(TimeSpan.FromMilliseconds(50));
				Progress = ((double)i) / 10;
				this.GenerateOutput("Hi number + " + i.ToString());
			}

			// no permission for this
			//var inst = Environment.NewServiceInstance(this.Configuration);
			//inst.Start();

			//throw new InvalidOperationException("Can't do this shit here.");

			return ServiceOutcome.Success;
		}
	}
}
