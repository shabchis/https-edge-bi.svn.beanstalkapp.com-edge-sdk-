using Edge.Core.Services;
using Edge.Data.Pipeline.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Edge.SDK.TestPipeline.Services
{
	public class MyBackofficeRetrieverService : PipelineService
	{
		protected override ServiceOutcome DoPipelineWork()
		{
			var dir = Configuration.Parameters.Get<string>("FileDirectory");
			foreach (var file in Delivery.Files)
			{
				file.Location = Path.Combine(String.Format(@"C:\Development\Edge.bi\Files\Deliveries\{0}", dir), file.Name);
			}
			Delivery.Save();
			return ServiceOutcome.Success;
		}
	}
}
