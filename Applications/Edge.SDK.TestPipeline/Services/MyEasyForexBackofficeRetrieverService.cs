using System.IO;
using Edge.Core.Services;
using Edge.Data.Pipeline.Services;

namespace Edge.SDK.TestPipeline.Services
{
	public class MyEasyForexBackofficeRetrieverService : PipelineService
	{
		protected override ServiceOutcome DoPipelineWork()
		{
			foreach (var file in Delivery.Files)
			{
				file.Location = Path.Combine(@"C:\Development\Edge.bi\Files\EasyForex\BackofficeData", file.Name);
			}
			Delivery.Save();
			return ServiceOutcome.Success;
		}
	}
}
