using System;
using System.Collections.Generic;
using System.Linq;
using Edge.Core.Services2;
using Edge.Core.Services2.Scheduling;
using Edge.Core.Services2.Workflow;
using WF = Edge.Core.Services2.Workflow;

abstract class EdgeServicesConfiguration
{
	public List<ServiceConfiguration> Services = new List<ServiceConfiguration>();
	public List<ServiceProfile> Profiles = new List<ServiceProfile>();
}

class Config: EdgeServicesConfiguration
{
	Config()
	{
		// ..............................
		// Adwords
		var adwords = new WorkflowServiceConfiguration()
		{
			Parameters = new Dictionary<string, object>()
			{
				{"MaxFileSize", 2048}
			}
		};

		adwords.Workflow.Nodes = new List<WorkflowNode>()
		{
			new WF.Group()
			{
				Name="Initializers",
				Mode = WF.GroupMode.Parallel,
				FailureBehavior = WorkflowNodeFailureBehavior.Terminate,
				Nodes = new List<WorkflowNode>()
				{
					new WF.Step() { Name="Initializer1", Service = new ServiceConfiguration(){} },
					new WF.Step() { Name="Initializer2", Service = new ServiceConfiguration(){} },
					new WF.Step() { Name="Initializer3", Service = new ServiceConfiguration(){} },
				}
			},
			
			// Validate initializers	
			new WF.If(n => n.Children.Count(child => child.Instance.Outcome == ServiceOutcome.Failure) > 0)
			{
				Then = new WF.End(ServiceOutcome.Failure)
			},
				
			new Step() { Name="Retriever", Service = new ServiceConfiguration() {} },
			new Step() { Name="Processor", Service = new ServiceConfiguration() {} }
		};
		Services.Add(adwords);

		// =================================================================
		// Profiles

		// ..............................
		Profiles.Add(new ServiceProfile()
		{
			Parameters = new Dictionary<string, object>()
			{
				{"AccountID", 100035},
				{"MaxFileSize", 500000}
			},
			Services = new List<ServiceConfiguration>()
			{
				adwords,
				new ServiceConfiguration() { BaseConfiguration = adwords } 
			}
		});
	}
}



