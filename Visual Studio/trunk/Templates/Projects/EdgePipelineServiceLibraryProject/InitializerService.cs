using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Edge.Core.Services;
using Edge.Data.Pipeline;
using Edge.Data.Pipeline.Deliveries;
using Edge.Data.Pipeline.Services;

namespace $safeprojectname$
{
	public class InitializerService: PipelineService
	{
		protected override ServiceOutcome DoPipelineWork()
		{
			// Initialize a new delivery
			this.Delivery = new Delivery(this.Instance.InstanceID)
			{
				// (optional) Get the account ID from the profile parameters
				AccountID = (int) this.Instance.Profile.Parameters["AccountID"],

				// (optional) Get the target period from the configuration parameters
				TargetPeriod = this.PipelineParameters.TargetPeriod
			};

			// TODO: add delivery files using this.Delivery.Files.Add

			return ServiceOutcome.Success;
		}
	}
}
