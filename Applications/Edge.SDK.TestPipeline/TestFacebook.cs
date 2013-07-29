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
									new WorkflowStep {Name = "EasyForexFacebookInitializer", ServiceConfiguration = GetInitializerConfig()},
									new WorkflowStep {Name = "EasyForexFacebookRetriever", ServiceConfiguration = GetRetrieverConfig()},
									new WorkflowStep {Name = "EasyForexFacebookProcessor", ServiceConfiguration = GetProcessorConfig()},
									new WorkflowStep {Name = "EasyForexFacebookTrasform", ServiceConfiguration = GetTransformConfig()},
									new WorkflowStep {Name = "EasyForexFacebookStaging", ServiceConfiguration = GetStagingConfig()},
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
				DeliveryID = GetGuidFromString("Delivery7_Facebook"),
				TimePeriod = GetTimePeriod(),
				Limits = { MaxExecutionTime = new TimeSpan(0, 1, 0, 0) }
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["Facebook.Account.ID"] = "52081533";
			config.Parameters["TimeZone"] = "-8";
			config.Parameters["Offset"] = "0";
			config.Parameters["Facebook.BaseServiceAdress"] = "https://graph.facebook.com";
			config.Parameters["FileDirectory"] = "EasyForexFacebook";
			return config;
		}

		private static ServiceConfiguration GetRetrieverConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(RetrieverService).AssemblyQualifiedName,
				//ServiceClass = typeof(MyEasyForexBackofficeRetrieverService).AssemblyQualifiedName,
				DeliveryID = GetGuidFromString("Delivery7_Facebook"),
				TimePeriod = GetTimePeriod(),
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) }
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["Facebook.AccessToken"] = "CAACZAMUPZCAd0BAC8C5u6ncZCQ9Q6VoBuxfkfocHlvM8fdbn5IDX90YXTaRKaW0IcgyiAZA3CqV80ELmpLZCGZCBfNlj36oSotTvjBw5r6lbXfU8UzawsRDi83UCZAAZClZAgGbP8X9qP86CiZCzeNh10D";
            
			return config;
		}

		private static ServiceConfiguration GetProcessorConfig()
		{
			var config = new AutoMetricsProcessorServiceConfiguration
			{
				ServiceClass = typeof(Edge.Services.Facebook.GraphApi.ProcessorService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) },
				DeliveryID = GetGuidFromString("Delivery7_Facebook"),
				DeliveryFileName = "EasyForexBackOffice",
				Compression = "None",
				ReaderAdapterType = "Edge.Data.Pipeline.XmlDynamicReaderAdapter, Edge.Data.Pipeline",
				MappingConfigPath = @"C:\Development\Edge.bi\Files\Facebook\Mapping\FacebookMapping.xml",
				SampleFilePath    =  @"C:\Development\Edge.bi\Files\Facebook\samples\AdGroupStats-sample.json"
			};

			// TODO shirat - check if should be a part of configuration class and not parameters
			config.Parameters["CreativeSampleFile"] = @"C:\Development\Edge.bi\Files\Facebook\samples\AdGroupsCreatives-sample.json";
			config.Parameters["CampaignSampleFile"] = @"C:\Development\Edge.bi\Files\Facebook\samples\Campaigns-sample.json";
			config.Parameters["AdGroupSampleFile"]  = @"C:\Development\Edge.bi\Files\Facebook\samples\AdGroups-sample.json";

			config.Parameters["ChecksumTheshold"] = "0.1";
			config.Parameters["Sql.TransformCommand"] = "SP_Delivery_Transform_BO_Generic(@DeliveryID:NvarChar,@DeliveryTablePrefix:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,?CommitTableName:NvarChar)";
			config.Parameters["Sql.StageCommand"] = "SP_Delivery_Rollback_By_DeliveryOutputID_v291(@DeliveryOutputID:NvarChar,@TableName:NvarChar)";
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["XPath"] = "Envelope/Body/GetGatewayStatisticsResponse/GetGatewayStatisticsResult/diffgram/DSMarketing/CampaignStatisticsForEasyNet";
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;

			return config;
		}

		private static ServiceConfiguration GetTransformConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(MetricsTransformService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) },
				DeliveryID = GetGuidFromString("Delivery7_Facebook"),
				MappingConfigPath = @"C:\Development\Edge.bi\Files\EasyForex\Mapping\Backoffice.xml",
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
				DeliveryID = GetGuidFromString("Delivery7_Facebook"),
				MappingConfigPath = @"C:\Development\Edge.bi\Files\EasyForex\Mapping\Backoffice.xml",
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
				Start = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.Start, BaseDateTime = DateTime.Today.AddDays(-1) },
				End = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.End, BaseDateTime = DateTime.Today.AddSeconds(-1) }
			};
			return period;
		}

		#endregion
	}
}
