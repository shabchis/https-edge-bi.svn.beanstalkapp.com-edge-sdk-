using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Edge.Core.Services;
using Edge.Data.Pipeline.Services;

namespace Edge.SDK.TestPipeline.Services
{
	public class MyGoogleAdWordsRetrieverService : PipelineService
	{
		protected override ServiceOutcome DoPipelineWork()
		{
			foreach (var file in Delivery.Files)
			{
				file.Location = Path.Combine(@"C:\Development\Edge.bi\Files\Adwords\Files", file.Name);
			}
			Delivery.Save();
			return ServiceOutcome.Success; ;
		}
	}
}
