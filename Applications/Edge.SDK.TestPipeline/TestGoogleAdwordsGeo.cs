using Edge.Core;
using Edge.Core.Services;
using Edge.Core.Services.Workflow;
using Edge.Data.Pipeline;
using Edge.Data.Pipeline.Metrics.Services;
using Edge.Data.Pipeline.Metrics.Services.Configuration;
using Edge.Data.Pipeline.Services;
using Edge.Services.Google.AdWords.Performance;
using System;

namespace Edge.SDK.TestPipeline
{
	public class TestGoogleAdwordsGeo : BaseTest
	{
		#region Main
		public void Test()
		{
			// do not clean for transform service
			//CleanDelivery();

			Init(CreateBaseWorkflow());
		}
		#endregion

		#region Configuration

		private ServiceConfiguration CreateBaseWorkflow()
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
									new WorkflowStep {Name = "GoogleAdwordsTestTrasform", ServiceConfiguration = GetTransformConfig()},
									new WorkflowStep {Name = "GoogleAdwordsTestStaging", ServiceConfiguration = GetStagingConfig()},
								}
				},
				Limits = { MaxExecutionTime = new TimeSpan(0, 3, 0, 0) }
			};
			return workflowConfig;
		}

		private ServiceConfiguration GetInitializerConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(InitializerService).AssemblyQualifiedName,
				DeliveryID = GetDeliveryId("Geo"),
				TimePeriod = GetTimePeriod(),
				Limits = { MaxExecutionTime = new TimeSpan(0, 1, 0, 0) }
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["FilterDeleted"] = false;
			config.Parameters["KeywordContentId"] = "111";
			config.Parameters["Adwords.MccEmail"] = ADWORDS_MCC_EMAIL;
			config.Parameters["Adwords.ClientID"] = ADWORDS_CLIENT_ID;
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

		private ServiceConfiguration GetRetrieverConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				//ServiceClass = typeof(MyGoogleAdWordsRetrieverService).AssemblyQualifiedName,
				ServiceClass = typeof(RetrieverService).AssemblyQualifiedName,
				DeliveryID = GetDeliveryId("Geo"),
				TimePeriod = GetTimePeriod(),
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) }
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["DeveloperToken"] = "5eCsvAOU06Fs4j5qHWKTCA";
			config.Parameters["Adwords.MccEmail"] = ADWORDS_MCC_EMAIL;
			config.Parameters["Adwords.ClientID"] = ADWORDS_CLIENT_ID;

			return config;
		}

		private ServiceConfiguration GetProcessorConfig()
		{
			var config = new AutoMetricsProcessorServiceConfiguration
			{
				ServiceClass = typeof(AutoMetricsProcessorService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) },
				DeliveryID = GetDeliveryId("Geo"),
				DeliveryFileName = "GEO_PERF",
				Compression = "Gzip",
				ReaderAdapterType = "Edge.Data.Pipeline.CsvDynamicReaderAdapter, Edge.Data.Pipeline",

				MappingConfigPath = String.Format(@"C:\Development\Edge.bi\Files\_Mapping\{0}\GoogleAdwordsMapping_Geo.xml", ACCOUNT_ID),
				SampleFilePath = String.Format(@"C:\Development\Edge.bi\Files\_Samples\{0}\Geo_sample.txt", ACCOUNT_ID),
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
			config.Parameters["Adwords.MccEmail"] = ADWORDS_MCC_EMAIL;
			config.Parameters["Adwords.ClientID"] = ADWORDS_CLIENT_ID;
			config.Parameters["EOF"] = "Total";
			config.Parameters["EOF_FieldName"] = "Day";

			return config;
		}

		private ServiceConfiguration GetTransformConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(MetricsTransformService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) },
				DeliveryID = GetDeliveryId("Geo"),
				MappingConfigPath = "",
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

		private ServiceConfiguration GetStagingConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(MetricsStagingService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 1, 0, 0) },
				DeliveryID = GetDeliveryId("Geo"),
				MappingConfigPath ="",
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

		private DateTimeRange? GetTimePeriod()
		{
			var period = new DateTimeRange
			{
				Start = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.Start, BaseDateTime = DateTime.Now.AddDays(-30) },
				End = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.End, BaseDateTime = DateTime.Now.AddDays(-1) }
			};
			return period;
		}

		#endregion
	}
}
