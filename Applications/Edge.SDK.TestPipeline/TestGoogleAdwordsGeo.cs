using Edge.Core;
using Edge.Core.Services;
using Edge.Core.Services.Workflow;
using Edge.Core.Utilities;
using Edge.Data.Pipeline;
using Edge.Data.Pipeline.Metrics.Services;
using Edge.Data.Pipeline.Metrics.Services.Configuration;
using Edge.Data.Pipeline.Services;
using Edge.Services.Google.AdWords.Performance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Edge.SDK.TestPipeline
{
	public class TestGoogleAdwordsGeo : BaseTest
	{
		#region Main
		public static void Test()
		{
			Init(CreateBaseWorkflow());

			// do not clean for transform service
			CleanDelivery();
		}
		#endregion

		#region Configuration

		private static ServiceConfiguration CreateBaseWorkflow()
		{
			var workflowConfig = new WorkflowServiceConfiguration
			{
				ServiceName = "GoogleAdwordsWorkflow",
				Workflow = new WorkflowNodeGroup
				{
					Mode = WorkflowNodeGroupMode.Linear,
					Nodes = new LockableList<WorkflowNode>
								{
									new WorkflowStep {Name = "GoogleAdwordsTestInitializer", ServiceConfiguration = GetInitializerConfig()},
									new WorkflowStep {Name = "GoogleAdwordsTestRetriever", ServiceConfiguration = GetRetrieverConfig()},
									new WorkflowStep {Name = "GoogleAdwordsTestProcessor", ServiceConfiguration = GetProcessorConfig()},
									//new WorkflowStep {Name = "GoogleAdwordsTestTrasform", ServiceConfiguration = GetTransformConfig()},
									//new WorkflowStep {Name = "GoogleAdwordsTestStaging", ServiceConfiguration = GetStagingConfig()},
								}
				},
				Limits = { MaxExecutionTime = new TimeSpan(0, 3, 0, 0) }
			};
			return workflowConfig;
		}

		private static ServiceConfiguration GetInitializerConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(InitializerService).AssemblyQualifiedName,
				DeliveryID = GetGuidFromString("Delivery7_Geo"),
				TimePeriod = GetTimePeriod(),
				Limits = { MaxExecutionTime = new TimeSpan(0, 1, 0, 0) }
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["FilterDeleted"] = false;
			config.Parameters["KeywordContentId"] = "111";
			config.Parameters["Adwords.MccEmail"] = "ppc.easynet@gmail.com";
			config.Parameters["Adwords.ClientID"] = "323-509-6780";
			config.Parameters["DeveloperToken"] = "5eCsvAOU06Fs4j5qHWKTCA";
			config.Parameters["SubChannelName"] = "sub";
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["includeZeroImpression"] = true;
			config.Parameters["Adwords.ReportConfig"] = @"
<GoogleAdwordsReportConfig>
  <Report Name='GEO_PERF' Type='GEO_PERFORMANCE_REPORT' Enable='true'>
    <Field Name='Date' />
    <Field Name='AdGroupId' />
	<Field Name='AdGroupName' />
	<Field Name='CampaignId' />
    <Field Name='CampaignName' />
    <Field Name='CountryCriteriaId' />
    <Field Name='RegionCriteriaId' />
	<Field Name='CityCriteriaId' />
	<Field Name='AccountCurrencyCode' />
	<Field Name='Ctr' />
	<Field Name='Impressions' />
	<Field Name='Clicks' />
	<Field Name='Cost' />
	<Field Name='AveragePosition' />
  </Report>
</GoogleAdwordsReportConfig>
";
			return config;
		}

		private static ServiceConfiguration GetRetrieverConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				//ServiceClass = typeof(MyGoogleAdWordsRetrieverService).AssemblyQualifiedName,
				ServiceClass = typeof(RetrieverService).AssemblyQualifiedName,
				DeliveryID = GetGuidFromString("Delivery7_Geo"),
				TimePeriod = GetTimePeriod(),
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) }
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["DeveloperToken"] = "5eCsvAOU06Fs4j5qHWKTCA";
			config.Parameters["Adwords.MccEmail"] = "ppc.easynet@gmail.com";
			config.Parameters["Adwords.ClientID"] = "323-509-6780";

			return config;
		}

		private static ServiceConfiguration GetProcessorConfig()
		{
			var config = new AutoMetricsProcessorServiceConfiguration
			{
				ServiceClass = typeof(AutoMetricsProcessorService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) },
				DeliveryID = GetGuidFromString("Delivery7_Geo"),
				DeliveryFileName = "GEO_PERF",
				Compression = "Gzip",
				ReaderAdapterType = "Edge.Data.Pipeline.CsvDynamicReaderAdapter, Edge.Data.Pipeline",

				MappingConfigPath = @"C:\Development\Edge.bi\Files\Adwords\Mapping\GoogleAdwordsMapping_Geo.xml",
				SampleFilePath = @"C:\Development\Edge.bi\Files\Adwords\Files\samples\Geo_sample.txt"
			};

			// TODO shirat - check if should be a part of configuration class and not parameters
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["ChecksumTheshold"] = "0.1";
			config.Parameters["Sql.TransformCommand"] = "SP_Delivery_Transform_BO_Generic(@DeliveryID:NvarChar,@DeliveryTablePrefix:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,?CommitTableName:NvarChar)";
			config.Parameters["Sql.StageCommand"] = "SP_Delivery_Rollback_By_DeliveryOutputID_v291(@DeliveryOutputID:NvarChar,@TableName:NvarChar)";
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["CsvDelimeter"] = ",";
			config.Parameters["CsvRequiredColumns"] = "Day";
			config.Parameters["CsvEncoding"] = "ASCII";
			config.Parameters["Adwords.SubChannelName"] = "subChannel";
			config.Parameters["Adwords.MccEmail"] = "ppc.easynet@gmail.com";
			config.Parameters["Adwords.ClientID"] = "323-509-6780";
			config.Parameters["EOF"] = "Total";
			config.Parameters["EOF_FieldName"] = "Day";

			return config;
		}

		private static ServiceConfiguration GetTransformConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(MetricsTransformService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) },
				DeliveryID = GetGuidFromString("Delivery7_Geo"),
				MappingConfigPath = @"C:\Development\Edge.bi\Files\temp\Mappings\1006\FtpAdvertising.xml",
			};

			// TODO shirat - check if should be a part of configuration class and not parameters
			config.Parameters["ChecksumTheshold"] = "0.1";
			config.Parameters["Sql.TransformCommand"] = "SP_Delivery_Transform_BO_Generic(@DeliveryID:NvarChar,@DeliveryTablePrefix:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,?CommitTableName:NvarChar)";
			config.Parameters["Sql.StageCommand"] = "SP_Delivery_Rollback_By_DeliveryOutputID_v291(@DeliveryOutputID:NvarChar,@TableName:NvarChar)";
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["IdentityInDebug"] = true;

			return config;
		}

		private static ServiceConfiguration GetStagingConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(MetricsStagingService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 1, 0, 0) },
				DeliveryID = GetGuidFromString("Delivery7_Geo"),
				MappingConfigPath = @"C:\Development\Edge.bi\Files\temp\Mappings\1006\FtpAdvertising.xml",
			};

			// TODO shirat - check if should be a part of configuration class and not parameters
			config.Parameters["ChecksumTheshold"] = "0.1";
			config.Parameters["Sql.TransformCommand"] = "SP_Delivery_Transform_BO_Generic(@DeliveryID:NvarChar,@DeliveryTablePrefix:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,?CommitTableName:NvarChar)";
			config.Parameters["Sql.StageCommand"] = "SP_Delivery_Rollback_By_DeliveryOutputID_v291(@DeliveryOutputID:NvarChar,@TableName:NvarChar)";
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["IdentityInDebug"] = true;
			config.Parameters["IdentityConfig"] = @"
<IdentityConfig>
  <EdgeType Name='Campaign'>
    <FieldToUpdate Name='Name' />
  </EdgeType>
  <EdgeType Name='AdGroup'>
    <FieldToUpdate Name='Value' />
  </EdgeType>
</IdentityConfig>";

			return config;
		}

		private static DateTimeRange? GetTimePeriod()
		{
			var period = new DateTimeRange
			{
				Start = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.Start, BaseDateTime = DateTime.Now.AddDays(-1) },
				End = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.End, BaseDateTime = DateTime.Now.AddDays(-5) }
			};
			return period;
		}

		#endregion
	}
}
