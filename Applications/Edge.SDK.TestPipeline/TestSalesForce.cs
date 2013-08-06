using System;
using Edge.Core;
using Edge.Core.Services;
using Edge.Core.Services.Workflow;
using Edge.Data.Pipeline;
using Edge.Data.Pipeline.Metrics.Services;
using Edge.Data.Pipeline.Metrics.Services.Configuration;
using Edge.Data.Pipeline.Services;
using Edge.SDK.TestPipeline.Services;
using Edge.Services.SalesForce;
using ProcessorService = Edge.Services.SalesForce.ProcessorService;

namespace Edge.SDK.TestPipeline
{
	public class TestSalesForce : BaseTest
	{
		#region Main
		public void Test()
		{
			// do not clean for transform or/and staging service
			CleanDelivery();

			Init(CreateBaseWorkflow());
		}
		#endregion

		#region Configuration

		private ServiceConfiguration CreateBaseWorkflow()
		{
			var workflowConfig = new WorkflowServiceConfiguration
			{
				ServiceName = "SaleForceWorkflow",
				Workflow = new WorkflowNodeGroup
				{
					Mode = WorkflowNodeGroupMode.Linear,
					Nodes = new LockableList<WorkflowNode>
								{
									new WorkflowStep {Name = "SaleForceTestInitializer", ServiceConfiguration = GetInitializerConfig()},
									new WorkflowStep {Name = "SaleForceTestRetriever", ServiceConfiguration = GetRetrieverConfig()},
									new WorkflowStep {Name = "SaleForceTestProcessor", ServiceConfiguration = GetProcessorConfig()},
									new WorkflowStep {Name = "SaleForceTestTrasform", ServiceConfiguration = GetTransformConfig()},
									new WorkflowStep {Name = "SaleForceTestStaging", ServiceConfiguration = GetStagingConfig()},
								}
				},
				Limits = { MaxExecutionTime = new TimeSpan(0, 3, 0, 0) }
			};
			return workflowConfig;
		}

		private ServiceConfiguration GetInitializerConfig()
		{
			var config = new AutoMetricsProcessorServiceConfiguration
			{
				ServiceClass = typeof(InitializerService).AssemblyQualifiedName,
				DeliveryID = GetDeliveryId("SaleForce"),
				TimePeriod = GetTimePeriod(),
				Limits = { MaxExecutionTime = new TimeSpan(0, 1, 0, 0) },
				DeliveryFileName = "GreenSqlDownload.json"
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;

			config.Parameters["AuthenticationUrl"] = "https://login.salesforce.com/services/oauth2/token";
			config.Parameters["SalesForceClientID"] = "3MVG9yZ.WNe6byQCa824BURpmLmBd4uyCY_xsDDj9jE.681uxAfte9ACgrXVls_Z9UtHonJdBj_2_rQnmr9Ja";
			config.Parameters["ConsentCode"] = "aPrxZibfVBKPF9vKVR7Fp0etunfQjgh2zzk79EKwfwfaxP0hrJWUI1SgEM4.u23K55F8.L_ccQ%3D%3D";
			config.Parameters["ClientSecret"] = "8606650677336597746";
			config.Parameters["Redirect_URI"] = "http://localhost:8080/RestTest/oauth/_callback";
			config.Parameters["Query"] = 
@"Select Name, Edge_Tracker__c,Download_Date__c,Trail_Activation_Date__c From Trail__c 
WHERE (Edge_Tracker__c!=null AND Edge_Tracker__c>0) AND 
((CALENDAR_YEAR(Download_Date__c)={0} AND 
CALENDAR_MONTH(Download_Date__c)={1} AND 
DAY_IN_MONTH(Download_Date__c)={2}) 
OR (CALENDAR_YEAR(Trail_Activation_Date__c)={0} 
AND CALENDAR_MONTH(Trail_Activation_Date__c)={1} 
AND DAY_IN_MONTH(Trail_Activation_Date__c)={2}))";
			
			return config;
		}

		private ServiceConfiguration GetRetrieverConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(MyBackofficeRetrieverService).AssemblyQualifiedName,
				//ServiceClass = typeof(RetrieverService).AssemblyQualifiedName,
				DeliveryID = GetDeliveryId("SaleForce"),
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
				ServiceClass = typeof(ProcessorService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) },
				DeliveryID = GetDeliveryId("SaleForce"),
				DeliveryFileName = "GreenSqlDownload.json",

				MappingConfigPath = String.Format(@"C:\Development\Edge.bi\Files\_Mapping\{0}\SalesForce.xml", ACCOUNT_ID),
				SampleFilePath = String.Format(@"C:\Development\Edge.bi\Files\_Samples\{0}\SalesForce_sample.json", ACCOUNT_ID)
			};

			// TODO shirat - check if should be a part of configuration class and not parameters
			config.Parameters["ChecksumTheshold"] = "0.1";
			config.Parameters["Sql.TransformCommand"] = "SP_Delivery_Transform_BO_Generic(@DeliveryID:NvarChar,@DeliveryTablePrefix:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,?CommitTableName:NvarChar)";
			config.Parameters["Sql.StageCommand"] = "SP_Delivery_Rollback_By_DeliveryOutputID_v291(@DeliveryOutputID:NvarChar,@TableName:NvarChar)";
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;

			return config;
		}

		private ServiceConfiguration GetTransformConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(MetricsTransformService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) },
				DeliveryID = GetDeliveryId("SaleForce"),
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
				DeliveryID = GetDeliveryId("SaleForce"),
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
				Start = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.Start, BaseDateTime = new DateTime(2012, 11, 04) },
				End = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.End, BaseDateTime = new DateTime(2012, 11, 04) }
				//Start = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.Start, BaseDateTime = DateTime.Now.AddDays(-30) },
				//End = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.End, BaseDateTime = DateTime.Now.AddDays(-1) }
			};
			return period;
		}

		#endregion
	}
}
