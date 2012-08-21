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
				ServiceType = typeof(TestService).AssemblyQualifiedName
			};

			var profile = new ServiceProfile()
			{
				Name = "Doron"
			};
			profile.Parameters["AccountID"] = 10035;

			ServiceConfiguration profileService = profile.DeriveConfiguration(serviceTemplate);

			while (Console.ReadLine() != "exit")
			{
				ServiceInstance instance = host.Environment.NewServiceInstance(profileService);
				instance.StateChanged += new EventHandler(instance_StateChanged);
				instance.ProgressReported += new EventHandler(instance_ProgressReported);
				instance.OutcomeReported += new EventHandler(instance_OutcomeReported);
				instance.OutputGenerated += new EventHandler(instance_OutputGenerated);
				instance.Start();
			}

			/*
			ServiceConfiguration retrieverConfig = new ServiceConfiguration()
			{
				ServiceName = "FacebookRetrieverService",
			};
			retrieverConfig.Parameters["FacebookToken"] = 123;

			WorkflowServiceConfiguration workflowConfig = new WorkflowServiceConfiguration();

			workflowConfig.Workflow = new Group()
			{
				Mode = GroupMode.Linear,
				Nodes = new List<WorkflowNode>()
				{
			//		new Step() { Name = "Retriever", ServiceConfiguration =  }
				}
			};
			*/

			
		}

		static void instance_StateChanged(object sender, EventArgs e)
		{
			Console.WriteLine("state: " + ((ServiceInstance)sender).State.ToString());
		}

		static void instance_ProgressReported(object sender, EventArgs e)
		{
			Console.WriteLine("progress: " + ((ServiceInstance)sender).Progress.ToString());
		}

		static void instance_OutputGenerated(object sender, EventArgs e)
		{
			//Console.WriteLine("--------------------------------------------");
			Console.WriteLine("---------------> output: " + ((LogMessage)((ServiceInstance)sender).Output).Message); //((ServiceInstance)sender).Output.ToString());
			//Console.WriteLine("--------------------------------------------");
		}

		static void instance_OutcomeReported(object sender, EventArgs e)
		{
			
			Console.WriteLine("outcome: " + ((ServiceInstance)sender).Outcome.ToString());
			
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
			}

			var inst = Environment.NewServiceInstance(this.Configuration);
			inst.Start();

			//throw new InvalidOperationException("Can't do this shit here.");

			return ServiceOutcome.Success;
		}
	}
}
