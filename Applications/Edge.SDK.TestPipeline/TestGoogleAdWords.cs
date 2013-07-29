using System;
using Edge.Core;
using Edge.Core.Services;
using Edge.Core.Services.Workflow;
using Edge.Core.Utilities;
using Edge.Data.Pipeline;
using Edge.Data.Pipeline.Metrics.Services;
using Edge.Data.Pipeline.Metrics.Services.Configuration;
using Edge.Data.Pipeline.Services;
using Edge.Services.Google.AdWords.Performance;
using ProcessorService = Edge.Services.Google.AdWords.Performance.ProcessorService;

namespace Edge.SDK.TestPipeline
{
	public class TestGoogleAdWords : BaseTest
	{

		#region Main
		public static void Test()
		{
			// do not clean for transform or/and staging service
			CleanDelivery();

			Init(CreateBaseWorkflow());
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
									new WorkflowStep {Name = "GoogleAdwordsTestTrasform", ServiceConfiguration = GetTransformConfig()},
									new WorkflowStep {Name = "GoogleAdwordsTestStaging", ServiceConfiguration = GetStagingConfig()},
								}
						},
					Limits = {MaxExecutionTime = new TimeSpan(0, 3, 0, 0)}
				};
			return workflowConfig;
		}

		private static ServiceConfiguration GetInitializerConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(InitializerService).AssemblyQualifiedName,
				DeliveryID = GetDeliveryId("Adwords"),
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
			config.Parameters["Adwords.ReportType"] = "KEYWORDS_PERFORMANCE_REPORT|AD_PERFORMANCE_REPORT|PLACEMENT_PERFORMANCE_REPORT";
			config.Parameters["IncludeStatus"] = true;
			config.Parameters["includeConversionTypes"] = true;
			config.Parameters["includeZeroImpression"] = true;
			config.Parameters["includeDisplaytData"] = true;
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
  <Report Name='KEYWORDS_PERF' Type='KEYWORDS_PERFORMANCE_REPORT' Enable='true'>
    <Field Name='Id' />
    <Field Name='AdGroupId' />
    <Field Name='CampaignId' />
    <Field Name='KeywordText' />
    <Field Name='KeywordMatchType' />
	<Field Name='Impressions' />
	<Field Name='Clicks' />
	<Field Name='Cost' />
	<Field Name='Status' />
	<Field Name='DestinationUrl' />
	<Field Name='QualityScore' />
  </Report>
  <Report Name='KEYWORDS_PERF_Status' Type='KEYWORDS_PERFORMANCE_REPORT' Enable='false'>
    <Field Name='Id' />
    <Field Name='AdGroupId' />
    <Field Name='CampaignId' />
    <Field Name='Status' />
	</Report>
  <Report Name='AD_PERF' Type='AD_PERFORMANCE_REPORT' Enable='true'>
    <Field Name='Id' />
    <Field Name='Date' />
    <Field Name='AdType' />
    <Field Name='AdGroupId' />
	<Field Name='AdGroupName' />
	<Field Name='AdGroupStatus' />
    <Field Name='CampaignId' />
    <Field Name='CampaignName' />
    <Field Name='CampaignStatus' />
    <Field Name='Headline' />
    <Field Name='Description1' />
	<Field Name='Description2' />
	<Field Name='KeywordId' />
	<Field Name='DisplayUrl' />
	<Field Name='CreativeDestinationUrl' />
	<Field Name='AccountTimeZoneId' />
	<Field Name='AccountCurrencyCode' />
	<Field Name='Ctr' />
	<Field Name='Status' />
	<Field Name='DevicePreference' />
	<Field Name='Impressions' />
	<Field Name='Clicks' />
	<Field Name='Cost' />
	<Field Name='AveragePosition' />
	<Field Name='Conversions' />
	<Field Name='ConversionRate' />
	<Field Name='ConversionRateManyPerClick' />
	<Field Name='ConversionsManyPerClick' />
	<Field Name='ConversionValue' />
	<Field Name='TotalConvValue' />
  </Report>
  <Report Name='AD_PERF_Conv' Type='AD_PERFORMANCE_REPORT' Enable='true'>
    <Field Name='Id' />
    <Field Name='Date' />
    <Field Name='KeywordId' />
	<Field Name='ConversionsManyPerClick' />
	<Field Name='ConversionCategoryName' />
  </Report>
  <Report Name='AD_PERF_Status' Type='AD_PERFORMANCE_REPORT' Enable='false'>
    <Field Name='Id' />
    <Field Name='Status' />
	<Field Name='AdGroupId' />
	<Field Name='AdGroupName' />
	<Field Name='AdGroupStatus' />
	<Field Name='CampaignId' />
	<Field Name='CampaignName' />
	<Field Name='CampaignStatus' />
  </Report>
  <Report Name='MANAGED_PLAC_PERF' Type='PLACEMENT_PERFORMANCE_REPORT' Enable='true'>
    <Field Name='Id' />
    <Field Name='AdGroupId' />
    <Field Name='CampaignId' />
    <Field Name='Status' />
	<Field Name='DestinationUrl' />
	<Field Name='PlacementUrl' />
	<Field Name='Clicks' />
  </Report>
  <Report Name='MANAGED_PLAC_PERF_Status' Type='PLACEMENT_PERFORMANCE_REPORT' Enable='false'>
    <Field Name='Id' />
    <Field Name='AdGroupId' />
    <Field Name='CampaignId' />
    <Field Name='Status' />
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
				DeliveryID = GetDeliveryId("Adwords"),
				TimePeriod = GetTimePeriod(),
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) }
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["DeveloperToken"] = "5eCsvAOU06Fs4j5qHWKTCA";
			config.Parameters["Adwords.MccEmail"] = ADWORDS_MCC_EMAIL;
			config.Parameters["Adwords.ClientID"] = ADWORDS_CLIENT_ID;

			return config;
		}

		private static ServiceConfiguration GetProcessorConfig()
		{
			var config = new AutoMetricsProcessorServiceConfiguration
			{
				ServiceClass = typeof(ProcessorService).AssemblyQualifiedName,
				Limits = {MaxExecutionTime = new TimeSpan(0, 2, 0, 0)},
				DeliveryID = GetDeliveryId("Adwords"),
				DeliveryFileName = "temp.txt",
				Compression = "None",
				ReaderAdapterType = "Edge.Data.Pipeline.CsvDynamicReaderAdapter, Edge.Data.Pipeline",

				MappingConfigPath = String.Format(@"C:\Development\Edge.bi\Files\_Mapping\{0}\GoogleAdwordsMapping.xml", ACCOUNT_ID),
				SampleFilePath = ""
			};

			// TODO shirat - check if should be a part of configuration class and not parameters
			config.Parameters["ChecksumTheshold"] = "0.1";
			config.Parameters["Sql.TransformCommand"] = "SP_Delivery_Transform_BO_Generic(@DeliveryID:NvarChar,@DeliveryTablePrefix:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,?CommitTableName:NvarChar)";
			config.Parameters["Sql.StageCommand"] = "SP_Delivery_Rollback_By_DeliveryOutputID_v291(@DeliveryOutputID:NvarChar,@TableName:NvarChar)";
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["CsvDelimeter"] = "\t";
			config.Parameters["CsvRequiredColumns"] = "Day_Code";
			config.Parameters["CsvEncoding"] = "ASCII";
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["KeywordSampleFile"] = String.Format(@"C:\Development\Edge.bi\Files\_Samples\{0}\Keyword_sample.txt", ACCOUNT_ID);
			config.Parameters["PlacementSampleFile"] = String.Format(@"C:\Development\Edge.bi\Files\_Samples\{0}\Placement_sample.txt", ACCOUNT_ID);
			config.Parameters["AdSampleFile"] = String.Format(@"C:\Development\Edge.bi\Files\_Samples\{0}\Ad_sample.txt", ACCOUNT_ID);
			config.Parameters["Adwords.MccEmail"] = ADWORDS_MCC_EMAIL;
			config.Parameters["Adwords.ClientID"] = ADWORDS_CLIENT_ID;
			config.Parameters["Adwords.SubChannelName"] = "subChannel";

			return config;
		}

		private static ServiceConfiguration GetTransformConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(MetricsTransformService).AssemblyQualifiedName,
				Limits = {MaxExecutionTime = new TimeSpan(0, 2, 0, 0)},
				DeliveryID = GetDeliveryId("Adwords"),
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

		private static ServiceConfiguration GetStagingConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(MetricsStagingService).AssemblyQualifiedName,
				Limits = {MaxExecutionTime = new TimeSpan(0, 1, 0, 0)},
				DeliveryID = GetDeliveryId("Adwords"),
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

		private static DateTimeRange? GetTimePeriod()
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
