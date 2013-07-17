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
using Edge.Data.Pipeline.Metrics.Misc;
using Edge.Data.Pipeline.Metrics.Services;
using Edge.Data.Pipeline.Metrics.Services.Configuration;
using Edge.Data.Pipeline.Services;
using Edge.Services.Google.AdWords;

namespace Edge.SDK.TestPipeline
{
	public class TestObjectsUpdate : BaseTest
	{
		#region Main
		public static void Test()
		{
			log4net.Config.XmlConfigurator.Configure();
			Log.Start();

			Log.Write("TestGoogleAdWords", "Starting Google Adwords Test", LogMessageType.Debug);

			var environment = CreateEnvironment();
			CleanEnv(environment);
			// do not clean for transform service
			CleanDelivery();

			var profileServiceConfig = CreatePipelineWorkflow();

			using (new ServiceExecutionHost(environment.EnvironmentConfiguration.DefaultHostName, environment))
			{
				using (var listener = environment.ListenForEvents(ServiceEnvironmentEventType.ServiceRequiresScheduling))
				{
					listener.ServiceRequiresScheduling += Environment_ServiceRequiresScheduling;

					do
					{
						// create and start service
						var instance = environment.NewServiceInstance(profileServiceConfig);
						instance.StateChanged += instance_StateChanged;
						instance.OutputGenerated += instance_OutputGenerated;
						instance.Start();
					} while (Console.ReadKey().Key != ConsoleKey.Escape);
				}
			}
		}
		#endregion

		#region Configuration
		private static ServiceConfiguration CreatePipelineWorkflow()
		{
			var workflowConfig = CreateBaseWorkflow();

			var profile = new ServiceProfile { Name = "GoogleProfile" };
			profile.Parameters["AccountID"] = 7;
			profile.Parameters["ChannelID"] = 1;
			profile.Parameters["FileDirectory"] = "Google";
			profile.Parameters["DeliveryFileName"] = "temp.txt";
			profile.Parameters["SourceUrl"] = "http://google.com";
			profile.Parameters["UsePassive"] = true;
			profile.Parameters["UseBinary"] = false;
			profile.Parameters["UserID"] = "edgedev";
			profile.Parameters["Password"] = "6719AEDC8CD5CC31B9931A7B0CEE1FF7";

			return profile.DeriveConfiguration(workflowConfig);
		}

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
				Limits = { MaxExecutionTime = new TimeSpan(0, 3, 0, 0) }
			};
			return workflowConfig;
		}

		private static ServiceConfiguration GetInitializerConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(InitializerService).AssemblyQualifiedName,
				DeliveryID = GetGuidFromString("Delivery7"),
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
			//config.Parameters["IncludeStatus"] = true;  
			//config.Parameters["includeConversionTypes"] = true;
			//config.Parameters["includeDisplaytData"] = true;
			config.Parameters["Adwords.ReportConfig"] = @"
<GoogleAdwordsReportConfig>
  <Report Name='CAMPAIGN_STATUS' Type='CAMPAIGN_PERFORMANCE_REPORT' Enable='true'>
    <Field Name='CampaignId' />
    <Field Name='CampaignName' />
    <Field Name='CampaignStatus' />
    <Field Name='TotalBudget' />
	<Field Name='Period' />
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
				DeliveryID = GetGuidFromString("Delivery7"),
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
				DeliveryID = GetGuidFromString("Delivery7"),
				DeliveryFileName = "CAMPAIGN_STATUS",
				Compression = "Gzip",
				ReaderAdapterType = "Edge.Data.Pipeline.CsvDynamicReaderAdapter, Edge.Data.Pipeline",

				MappingConfigPath = @"C:\Development\Edge.bi\Files\Adwords\Mapping\CampaignStatusMapping.xml",
				SampleFilePath = @"C:\Development\Edge.bi\Files\Adwords\Files\samples\CampaignStatus_sample.txt"
			};

			// TODO shirat - check if should be a part of configuration class and not parameters
			config.Parameters["ChecksumTheshold"] = "0.1";
			config.Parameters["Sql.TransformCommand"] = "SP_Delivery_Transform_BO_Generic(@DeliveryID:NvarChar,@DeliveryTablePrefix:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,?CommitTableName:NvarChar)";
			config.Parameters["Sql.StageCommand"] = "SP_Delivery_Rollback_By_DeliveryOutputID_v291(@DeliveryOutputID:NvarChar,@TableName:NvarChar)";
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["CsvDelimeter"] = ",";
			config.Parameters["CsvRequiredColumns"] = "Campaign";
			config.Parameters["CsvEncoding"] = "ASCII";
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["KeywordSampleFile"] = @"C:\Development\Edge.bi\Files\Adwords\Files\samples\Keyword_sample.txt";
			config.Parameters["AdSampleFile"] = @"C:\Development\Edge.bi\Files\Adwords\Files\samples\Ad_sample.txt";
			config.Parameters["Adwords.MccEmail"] = "ppc.easynet@gmail.com";
			config.Parameters["Adwords.ClientID"] = "323-509-6780";
			config.Parameters["Adwords.SubChannelName"] = "subChannel";
			config.Parameters["EOF"] = "Total";
			config.Parameters["EOF_FieldName"] = "Campaign ID";

			return config;
		}

		private static ServiceConfiguration GetTransformConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(MetricsTransformService).AssemblyQualifiedName,
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) },
				DeliveryID = GetGuidFromString("Delivery7"),
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
				DeliveryID = GetGuidFromString("Delivery7"),
				MappingConfigPath = @"C:\Development\Edge.bi\Files\temp\Mappings\1006\FtpAdvertising.xml",
			};

			// TODO shirat - check if should be a part of configuration class and not parameters
			config.Parameters["ChecksumTheshold"] = "0.1";
			config.Parameters["Sql.TransformCommand"] = "SP_Delivery_Transform_BO_Generic(@DeliveryID:NvarChar,@DeliveryTablePrefix:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,?CommitTableName:NvarChar)";
			config.Parameters["Sql.StageCommand"] = "SP_Delivery_Rollback_By_DeliveryOutputID_v291(@DeliveryOutputID:NvarChar,@TableName:NvarChar)";
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["IdentityInDebug"] = true;
			//config.Parameters["CreateNewEdgeObjects"] = false;

			return config;
		}

		private static DateTimeRange? GetTimePeriod()
		{
			var period = new DateTimeRange
			{
				Start = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.Start, BaseDateTime = DateTime.Now.AddDays(-1) },
				End = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.End, BaseDateTime = DateTime.Now.AddDays(-1) }
			};
			return period;
		}

		private static ServiceEnvironment CreateEnvironment()
		{
			// create service env
			var envConfig = new ServiceEnvironmentConfiguration
			{
				DefaultHostName = "Shira",
				ConnectionString = "Data Source=bi_rnd;Initial Catalog=EdgeSystem;Integrated Security=true",
				SP_HostListGet = "Service_HostList",
				SP_HostRegister = "Service_HostRegister",
				SP_HostUnregister = "Service_HostUnregister",
				SP_InstanceSave = "Service_InstanceSave",
				SP_InstanceGet = "Service_InstanceGet",
				SP_InstanceReset = "Service_InstanceReset",
				SP_EnvironmentEventListenerListGet = "Service_EnvironmentEventListenerListGet",
				SP_EnvironmentEventListenerRegister = "Service_EnvironmentEventListenerRegister",
				SP_EnvironmentEventListenerUnregister = "Service_EnvironmentEventListenerUnregister",
				SP_ServicesExecutionStatistics = "Service_ExecutionStatistics_GetByPercentile",
				SP_InstanceActiveListGet = "Service_InstanceActiveList_GetByTime"
			};

			var environment = ServiceEnvironment.Open("Pipeline Test", envConfig);

			return environment;
		}

		#endregion

		#region Events
		private static void Environment_ServiceRequiresScheduling(object sender, ServiceInstanceEventArgs e)
		{
			Console.WriteLine("     --> child of: {0}", e.ServiceInstance.ParentInstance != null ? e.ServiceInstance.ParentInstance.Configuration.ServiceName : "(no parent)");
			e.ServiceInstance.StateChanged += instance_StateChanged;
			e.ServiceInstance.OutputGenerated += instance_OutputGenerated;
			e.ServiceInstance.Start();
		}

		static void instance_StateChanged(object sender, EventArgs e)
		{
			var instance = (ServiceInstance)sender;
			Console.WriteLine("{3} ({4}) -- state: {0}, progress: {1}, outcome: {2}", instance.State, instance.Progress, instance.Outcome, instance.Configuration.ServiceName, instance.InstanceID.ToString("N").Substring(0, 4));
		}

		static void instance_OutputGenerated(object sender, ServiceOutputEventArgs e)
		{
			Console.WriteLine("     --> " + e.Output);
		}
		#endregion
	}
}
