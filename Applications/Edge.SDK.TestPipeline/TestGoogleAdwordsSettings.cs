using System;
using Edge.Core;
using Edge.Core.Services;
using Edge.Core.Services.Workflow;
using Edge.Data.Pipeline;
using Edge.Data.Pipeline.Metrics.Services;
using Edge.Data.Pipeline.Metrics.Services.Configuration;
using Edge.Data.Pipeline.Services;
using Edge.Services.Google.AdWords.Settings;

namespace Edge.SDK.TestPipeline
{
	public class TestGoogleAdwordsSettings : BaseTest
	{
		#region Main
		public void Test()
		{
			// do not clean for transform service
			CleanDelivery();

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
									new WorkflowStep {Name = "GoogleAdwordsSettingTestInitializer", ServiceConfiguration = GetInitializerConfig()},
									new WorkflowStep {Name = "GoogleAdwordsSettingTestRetriever", ServiceConfiguration = GetRetrieverConfig()},
									new WorkflowStep {Name = "GoogleAdwordsSettingTestProcessor", ServiceConfiguration = GetProcessorConfig()},
									new WorkflowStep {Name = "GoogleAdwordsSettingTestTrasform", ServiceConfiguration = GetTransformConfig()},
									new WorkflowStep {Name = "GoogleAdwordsSettingTestStaging", ServiceConfiguration = GetStagingConfig()},
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
				DeliveryID = GetDeliveryId("Settings"),
				TimePeriod = GetTimePeriod(),
				Limits = { MaxExecutionTime = new TimeSpan(0, 1, 0, 0) }
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["Adwords.MccEmail"] = ADWORDS_MCC_EMAIL;
			config.Parameters["Adwords.ClientID"] = ADWORDS_CLIENT_ID;
			config.Parameters["DeveloperToken"] = "5eCsvAOU06Fs4j5qHWKTCA";
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["Adwords.ReportConfig"] = @"
<GoogleAdwordsReportConfig>
  <Report Name='CampaignCriterion' Type='CampaignCriterion' Filter='LOCATION|LANGUAGE' Enable='true'>
    <Field Name='Id' />
    <Field Name='CriteriaType' />
    <Field Name='IsNegative' />
    <Field Name='CampaignId' />
    <Field Name='LanguageCode' />
    <Field Name='LanguageName' />
    <Field Name='LocationName' />
    <Field Name='DisplayType' />
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
				DeliveryID = GetDeliveryId("Settings"),
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
				ServiceClass = typeof(CampaignCriterionProcessorService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) },
				DeliveryID = GetDeliveryId("Settings"),
				DeliveryFileName = "CampaignCriterion",
				Compression = "None",
				ReaderAdapterType = "Edge.Data.Pipeline.CsvDynamicReaderAdapter, Edge.Data.Pipeline",

				MappingConfigPath = String.Format(@"C:\Development\Edge.bi\Files\_Mapping\{0}\CampaignCriterionMapping.xml", ACCOUNT_ID),
				SampleFilePath = ""
			};

			// TODO shirat - check if should be a part of configuration class and not parameters
			config.Parameters["ChecksumTheshold"] = "0.1";
			config.Parameters["Sql.TransformCommand"] = "SP_Delivery_Transform_BO_Generic(@DeliveryID:NvarChar,@DeliveryTablePrefix:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,?CommitTableName:NvarChar)";
			config.Parameters["Sql.StageCommand"] = "SP_Delivery_Rollback_By_DeliveryOutputID_v291(@DeliveryOutputID:NvarChar,@TableName:NvarChar)";
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["CsvDelimeter"] = ",";
			config.Parameters["CsvRequiredColumns"] = "Id";
			config.Parameters["CsvEncoding"] = "ASCII";
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;

			return config;
		}

		private ServiceConfiguration GetTransformConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(MetricsTransformService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) },
				DeliveryID = GetDeliveryId("Settings"),
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
				DeliveryID = GetDeliveryId("Settings"),
				MappingConfigPath = "",
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
  <EdgeType Name='Campaign' CreateNewObjects='false'>
    <FieldToUpdate Name='Location' />
    <FieldToUpdate Name='IsLocationNegative' />
    <FieldToUpdate Name='Language' />
    <FieldToUpdate Name='IsLanguageNegative' />
  </EdgeType>
</IdentityConfig>";

			return config;
		}

		private DateTimeRange? GetTimePeriod()
		{
			var period = new DateTimeRange
			{
				Start = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.Start, BaseDateTime = DateTime.Now.AddDays(-1) },
				End = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.End, BaseDateTime = DateTime.Now.AddDays(-1) }
			};
			return period;
		}

		#endregion
	}
}
