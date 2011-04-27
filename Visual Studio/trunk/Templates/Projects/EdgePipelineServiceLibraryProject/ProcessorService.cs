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
	public class ProcessorService: PipelineService
	{
		protected override ServiceOutcome DoPipelineWork()
		{
			// TODO:
			//	- Use FileManager.Open to create a new reader (e.g. XmlDynamicReader or CsvDynamicReader, or a custom reader),
			//	- Read the contents row by row
			//	- Import each row data using a new import session (e.g. AdDataImportSession, or a custom DeliveryImportSession<T>)
			throw new NotImplementedException();
		}
	}
}
