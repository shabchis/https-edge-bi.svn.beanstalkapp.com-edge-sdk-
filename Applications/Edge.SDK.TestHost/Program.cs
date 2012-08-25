using System;
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

			//var env = new ServiceEnvironment();
			var host = new ServiceExecutionHost();

			// ..........................................................
			// STEP 2 - service

			var serviceTemplate = new ServiceConfiguration()
			{
				IsEnabled = true,
				ServiceName = "TestService",
				ServiceClass = typeof(TestService).AssemblyQualifiedName
			};
			WorkflowServiceConfiguration workflowConfig = new WorkflowServiceConfiguration();
			workflowConfig.Workflow = new Group()
			{
				Mode = GroupMode.Linear,
				Nodes = new LockableList<WorkflowNode>()
				{
					//		new Step() { Name = "Retriever", ServiceConfiguration =  }
				}
			};

			var profile = new ServiceProfile()
			{
				Name = "Doron"
			};
			profile.Parameters["AccountID"] = 10035;

			ServiceConfiguration profileService = profile.DeriveConfiguration(workflowConfig);

			do
			{
				ServiceInstance instance = host.Environment.NewServiceInstance(profileService);
				instance.StateChanged += new EventHandler(instance_StateChanged);
				instance.OutputGenerated += new EventHandler<ServiceOutputEventArgs>(instance_OutputGenerated);
				instance.Start();
			} while (Console.ReadLine() != "exit");

			/*
			ServiceConfiguration retrieverConfig = new ServiceConfiguration()
			{
				ServiceName = "FacebookRetrieverService",
			};
			retrieverConfig.Parameters["FacebookToken"] = 123;
			*/
			
			

			
		}

		static void instance_StateChanged(object sender, EventArgs e)
		{
			var instance = (ServiceInstance) sender;
			Console.WriteLine("{3} -- state: {0}, progress: {1}, outcome: {2}", instance.State, instance.Progress, instance.Outcome, instance.InstanceID.ToString("N").Substring(0,4));
		}


		static void instance_OutputGenerated(object sender, ServiceOutputEventArgs e)
		{
			Console.WriteLine("--------------->" + e.Output.ToString());
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
				this.GenerateOutput(i);
			}

			// no permission for this
			//var inst = Environment.NewServiceInstance(this.Configuration);
			//inst.Start();

			//throw new InvalidOperationException("Can't do this shit here.");

			return ServiceOutcome.Success;
		}
	}
}
