using System;
using System.Collections.Generic;
using System.Linq;
using Edge.Core.Services;
using Edge.Data.Pipeline.Metrics.Managers;
using Edge.Data.Pipeline.Objects;
using Edge.Data.Pipeline;
using Edge.Data.Pipeline.Mapping;
using Edge.Data.Pipeline.Metrics.Services;

namespace Edge.SDK.TestPipeline.Services
{
	class ProcessorService : AutoMetricsProcessorService
	{
		DeliveryOutput _currentOutput;
		bool _isChecksum = false;

		public new MetricsDeliveryManager ImportManager
		{
			get { return base.ImportManager; }
			set { base.ImportManager = value; }
		}

		protected override void InitMappings()
		{
			base.InitMappings();
			Mappings.ExternalMethods.Add("IsChecksum", new Func<bool>(() => _isChecksum));	
			Mappings.Compile();
		}

		protected override ServiceOutcome DoPipelineWork()
		{
			MappingContainer metricsUnitMapping;
			if (!Mappings.Objects.TryGetValue(typeof(MetricsUnit), out metricsUnitMapping))
				throw new MappingConfigurationException("Missing mapping definition for GenericMetricsUnit.");

			_currentOutput = Delivery.Outputs.First();
			_currentOutput.Checksum = new Dictionary<string, double>();
			var columns = new Dictionary<string, int>();
			foreach (var reportFile in Delivery.Files)
			{

				//Get Columns
				var reportReader = new JsonDynamicReader(reportFile.OpenContents(), "$.columnHeaders[*].*");
				using (reportReader)
				{
					var colIndex = 0;
					while (reportReader.Read())
					{
						columns.Add(reportReader.Current.name, colIndex);
						colIndex++;
					}
				}

				// get sample unit to create metrics table
				reportReader = new JsonDynamicReader(reportFile.OpenContents(), "$.rows[*].*");
				using (reportReader)
				{
					Mappings.OnFieldRequired = field => reportReader.Current["array"][columns[field]];
					
					reportReader.Read();
					CurrentMetricsUnit = new MetricsUnit();
					metricsUnitMapping.Apply(CurrentMetricsUnit);

					using (ImportManager = new MetricsDeliveryManager(InstanceID, EdgeTypes, ExtraFields))
					{
						ImportManager.BeginImport(Delivery, CurrentMetricsUnit);

						//Get values
						reportReader = new JsonDynamicReader(reportFile.OpenContents(), "$.rows[*].*");
						using (reportReader)
						{
							Mappings.OnFieldRequired = field => reportReader.Current["array"][columns[field]];

							while (reportReader.Read())
							{
								CurrentMetricsUnit = new MetricsUnit();
								metricsUnitMapping.Apply(CurrentMetricsUnit);

								ProcessMetrics();
							}
						}

						// finish import (insert edge objects)
						ImportManager.EndImport();
					}
				}
			}
			return ServiceOutcome.Success;
		}
	}
}
