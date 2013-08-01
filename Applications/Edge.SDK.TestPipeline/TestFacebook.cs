using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using Edge.Core;
using Edge.Core.Configuration;
using Edge.Core.Services;
using Edge.Core.Services.Workflow;
using Edge.Core.Utilities;
using Edge.Data.Pipeline;
using Edge.Data.Pipeline.Metrics.Managers;
using Edge.Data.Pipeline.Metrics.Services;
using Edge.Data.Pipeline.Metrics.Services.Configuration;
using Edge.Data.Pipeline.Services;
using Edge.Services.Facebook.GraphApi;

namespace Edge.SDK.TestPipeline
{
	public class TestFacebook : BaseTest
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
				ServiceName = "FacebookWorkflow",
				Workflow = new WorkflowNodeGroup
				{
					Mode = WorkflowNodeGroupMode.Linear,
					Nodes = new LockableList<WorkflowNode>
								{
									new WorkflowStep {Name = "FacebookInitializer", ServiceConfiguration = GetInitializerConfig()},
									new WorkflowStep {Name = "FacebookRetriever", ServiceConfiguration = GetRetrieverConfig()},
									new WorkflowStep {Name = "FacebookProcessor", ServiceConfiguration = GetProcessorConfig()},
									new WorkflowStep {Name = "FacebookTrasform", ServiceConfiguration = GetTransformConfig()},
									new WorkflowStep {Name = "FacebookStaging", ServiceConfiguration = GetStagingConfig()},
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
				DeliveryID = GetGuidFromString("Facebook"),
				TimePeriod = GetTimePeriod(),
				Limits = { MaxExecutionTime = new TimeSpan(0, 1, 0, 0) }
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["Facebook.Account.ID"] = FACEBOOK_ACCOUNT_ID;
			config.Parameters["TimeZone"] = "2";
			config.Parameters["Offset"] = "0";
			config.Parameters["Facebook.BaseServiceAdress"] = "https://graph.facebook.com";
			config.Parameters["FileDirectory"] = GetTestName();
			return config;
		}

		private ServiceConfiguration GetRetrieverConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(RetrieverService).AssemblyQualifiedName,
				//ServiceClass = typeof(MyEasyForexBackofficeRetrieverService).AssemblyQualifiedName,
				DeliveryID = GetGuidFromString("Facebook"),
				TimePeriod = GetTimePeriod(),
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) }
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["Facebook.AccessToken"] = FACEBOOK_ACCESS_TOKEN;
            
			return config;
		}

		private ServiceConfiguration GetProcessorConfig()
		{
			var config = new AutoMetricsProcessorServiceConfiguration
			{
				ServiceClass = typeof(ProcessorService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) },
				DeliveryID = GetGuidFromString("Facebook"),
				DeliveryFileName = "facebook",
				Compression = "None",
				ReaderAdapterType = "Edge.Data.Pipeline.XmlDynamicReaderAdapter, Edge.Data.Pipeline",
				
				MappingConfigPath = String.Format(@"C:\Development\Edge.bi\Files\_Mapping\{0}\FacebookMapping.xml", ACCOUNT_ID),
				SampleFilePath = String.Format(@"C:\Development\Edge.bi\Files\_Samples\{0}\Facebook\AdGroupStats_sample.json", ACCOUNT_ID)
			};

			// TODO shirat - check if should be a part of configuration class and not parameters
			config.Parameters["CreativeSampleFile"] = String.Format(@"C:\Development\Edge.bi\Files\_Samples\{0}\Facebook\AdGroupsCreatives_sample.json", ACCOUNT_ID);
			config.Parameters["CampaignSampleFile"] = String.Format(@"C:\Development\Edge.bi\Files\_Samples\{0}\Facebook\Campaigns_sample.json", ACCOUNT_ID);
			config.Parameters["AdGroupSampleFile"]  = String.Format(@"C:\Development\Edge.bi\Files\_Samples\{0}\Facebook\AdGroups_sample.json", ACCOUNT_ID);

			config.Parameters["ChecksumTheshold"] = "0.1";
			config.Parameters["Sql.TransformCommand"] = "SP_Delivery_Transform_BO_Generic(@DeliveryID:NvarChar,@DeliveryTablePrefix:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,?CommitTableName:NvarChar)";
			config.Parameters["Sql.StageCommand"] = "SP_Delivery_Rollback_By_DeliveryOutputID_v291(@DeliveryOutputID:NvarChar,@TableName:NvarChar)";
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["XPath"] = "Envelope/Body/GetGatewayStatisticsResponse/GetGatewayStatisticsResult/diffgram/DSMarketing/CampaignStatisticsForEasyNet";
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;

			return config;
		}

		private ServiceConfiguration GetTransformConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(MetricsTransformService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) },
				DeliveryID = GetGuidFromString("Facebook"),
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
				DeliveryID = GetGuidFromString("Facebook"),
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

		private DateTimeRange? GetTimePeriod()
		{
			var period = new DateTimeRange
			{
				//Start = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.Start, BaseDateTime = new DateTime(2012, 11, 01) },
				//End = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.End, BaseDateTime = new DateTime(2012, 11, 30) }
				Start = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.Start, BaseDateTime = DateTime.Today.AddDays(-1) },
				End = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.End, BaseDateTime = DateTime.Today.AddSeconds(-1) }
			};
			return period;
		}

		#endregion
	}
}
