using System;
using Edge.Core;
using Edge.Core.Services;
using Edge.Core.Services.Workflow;
using Edge.Data.Pipeline;
using Edge.Data.Pipeline.Metrics.Services;
using Edge.Data.Pipeline.Metrics.Services.Configuration;
using Edge.Data.Pipeline.Services;

namespace Edge.SDK.TestPipeline
{
	public class TestEasyForexBackoffice : BaseTest
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
									new WorkflowStep {Name = "EasyForexBackofficeInitializer", ServiceConfiguration = GetInitializerConfig()},
									new WorkflowStep {Name = "EasyForexBackofficeRetriever", ServiceConfiguration = GetRetrieverConfig()},
									new WorkflowStep {Name = "EasyForexBackofficeProcessor", ServiceConfiguration = GetProcessorConfig()},
									new WorkflowStep {Name = "EasyForexBackofficeTrasform", ServiceConfiguration = GetTransformConfig()},
									new WorkflowStep {Name = "EasyForexBackofficeStaging", ServiceConfiguration = GetStagingConfig()},
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
				ServiceClass = typeof(Edge.Services.BackOffice.EasyForex.InitializerService).AssemblyQualifiedName,
				DeliveryID = GetGuidFromString("Delivery7"),
				TimePeriod = GetTimePeriod(),
				Limits = { MaxExecutionTime = new TimeSpan(0, 1, 0, 0) }
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["SourceUrl"] = "https://classic.easy-forex.com/BackOffice/API/Marketing.asmx";
			config.Parameters["SOAPAction"] = "http://www.easy-forex.com/GetGatewayStatistics";
			config.Parameters["SoapMethod"] = "GetGatewayStatistics";
			config.Parameters["StartGid"] = "1";
			config.Parameters["EndGid"] = "1000000";
			config.Parameters["User"] = "Seperia";
			config.Parameters["Pass"] = "B0D003A2CB1865E33AFD79CFD8761BA8";
			config.Parameters["FileDirectory"] = "EasyForexBackoffice";
			config.Parameters["Bo.Xpath"] ="Envelope/Body/GetGatewayStatisticsResponse/GetGatewayStatisticsResult/diffgram/DSMarketing/CampaignStatisticsForEasyNet";
			config.Parameters["Bo.IsAttribute"] = "false";
			config.Parameters["Bo.TrackerIDField"] = "GID";

			return config;
		}

		private ServiceConfiguration GetRetrieverConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(Edge.Services.BackOffice.EasyForex.RetrieverService).AssemblyQualifiedName,
				//ServiceClass = typeof(MyEasyForexBackofficeRetrieverService).AssemblyQualifiedName,
				DeliveryID = GetGuidFromString("Delivery7"),
				TimePeriod = GetTimePeriod(),
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) }
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;

			return config;
		}

		private ServiceConfiguration GetProcessorConfig()
		{
			var config = new AutoMetricsProcessorServiceConfiguration
			{
				ServiceClass = typeof(AutoMetricsProcessorService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) },
				DeliveryID = GetGuidFromString("Delivery7"),
				DeliveryFileName = "EasyForexBackOffice",
				Compression = "None",
				ReaderAdapterType = "Edge.Data.Pipeline.XmlDynamicReaderAdapter, Edge.Data.Pipeline",
				MappingConfigPath = @"C:\Development\Edge.bi\Files\EasyForex\Mapping\Backoffice.xml",
				SampleFilePath = @"C:\Development\Edge.bi\Files\EasyForex\Samples\EasyForexBackoffice-sample"
			};

			// TODO shirat - check if should be a part of configuration class and not parameters
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
				DeliveryID = GetGuidFromString("Delivery7"),
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

		private ServiceConfiguration GetStagingConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(MetricsStagingService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 1, 0, 0) },
				DeliveryID = GetGuidFromString("Delivery7"),
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

		private DateTimeRange? GetTimePeriod()
		{
			var period = new DateTimeRange
			{
				Start = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.Start, BaseDateTime = DateTime.Today.AddDays(-4) },
				End = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.End, BaseDateTime = DateTime.Today.AddSeconds(-4) }
			};
			return period;
		}

		#endregion
	}
}
