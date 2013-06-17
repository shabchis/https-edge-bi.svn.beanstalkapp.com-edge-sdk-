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
using ProcessorService = Edge.Services.Google.AdWords.ProcessorService;

namespace Edge.SDK.TestPipeline
{
	public class TestGoogleAdWords
	{
		#region Main

		static void Main()
		{
			// testing metrics viewer
			//using (var connection = new SqlConnection(AppSettings.GetConnectionString(typeof(MetricsDeliveryManager), Consts.ConnectionStrings.Objects)))
			//{
			//	connection.Open();
			//	var sql = EdgeViewer.GetMetricsView(3, "[DBO].[3__20130410_181916_5f368d7f48490b6484bcc9482b730dba_Metrics]", connection);
			//}

			// test EdgeTypes inheritence
			//using (var connection = new SqlConnection(AppSettings.GetConnectionString(typeof(MetricsDeliveryManager), Consts.ConnectionStrings.Objects)))
			//{
			//	connection.Open();
			//	var edgeTypes = EdgeObjectConfigLoader.LoadEdgeTypes(3, connection);
			//	var inheritors = EdgeObjectConfigLoader.FindEdgeTypeInheritors(edgeTypes.Values.FirstOrDefault(x => x.TypeID == 1),edgeTypes);
			//}

			log4net.Config.XmlConfigurator.Configure();
			Log.Start();

			Log.Write("TestGoogleAdWords", "Starting Google Adwords Test", LogMessageType.Debug);

			var environment = CreateEnvironment();
			// do not clean for transform service
			Clean(environment);

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
		public static ServiceConfiguration CreatePipelineWorkflow()
		{
			var workflowConfig = CreateBaseWorkflow();

			var profile = new ServiceProfile { Name = "GoogleProfile" };
			profile.Parameters["AccountID"] = 3;
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

		public static ServiceConfiguration CreateBaseWorkflow()
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
				DeliveryID = GetGuidFromString("Delivery2"),
				TimePeriod = GetTimePeriod(),
				Limits = { MaxExecutionTime = new TimeSpan(0, 1, 0, 0) }
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["FilterDeleted"] = false;
			config.Parameters["KeywordContentId"] = "111";
			config.Parameters["Adwords.MccEmail"] = "edge.bi.mcc@gmail.com";
			config.Parameters["Adwords.ClientID"] = "508-397-0423";
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
				DeliveryID = GetGuidFromString("Delivery2"),
				TimePeriod = GetTimePeriod(),
				Limits = { MaxExecutionTime = new TimeSpan(0, 2, 0, 0) }
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["DeveloperToken"] = "5eCsvAOU06Fs4j5qHWKTCA";
			config.Parameters["Adwords.MccEmail"] = "edge.bi.mcc@gmail.com";
			config.Parameters["Adwords.ClientID"] = "508-397-0423";

			return config;
		}

		private static ServiceConfiguration GetProcessorConfig()
		{
			//------------------------------------------
			// FtpAdvertesing sample (account 1006)
			//------------------------------------------
			var config = new AutoMetricsProcessorServiceConfiguration
			{
				ServiceClass = typeof(ProcessorService).AssemblyQualifiedName,
				Limits = {MaxExecutionTime = new TimeSpan(0, 2, 0, 0)},
				DeliveryID = GetGuidFromString("Delivery2"),
				DeliveryFileName = "temp.txt",
				Compression = "None",
				ReaderAdapterType = "Edge.Data.Pipeline.CsvDynamicReaderAdapter, Edge.Data.Pipeline",

				MappingConfigPath = @"C:\Development\Edge.bi\Files\Adwords\Mapping\GoogleAdwordsMapping.xml",
				SampleFilePath = @"C:\Development\Edge.bi\Files\temp\Mappings\1006\bBinary_Sample.txt"
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
			config.Parameters["KeywordSampleFile"] = @"C:\Development\Edge.bi\Files\Adwords\Files\samples\Keyword_sample.txt";
			config.Parameters["AdSampleFile"] = @"C:\Development\Edge.bi\Files\Adwords\Files\samples\Ad_sample.txt";
			config.Parameters["Adwords.MccEmail"] = "edge.bi.mcc@gmail.com";
			config.Parameters["Adwords.ClientID"] = "508-397-0423";
			config.Parameters["Adwords.SubChannelName"] = "subChannel";
			

			//------------------------------------------
			// Google Analytics sample (account 61)
			//------------------------------------------
			//var config = new AutoMetricsProcessorServiceConfiguration
			//{
			//	ServiceClass = typeof(ProcessorService).AssemblyQualifiedName,
			//	DeliveryID = GetGuidFromString("Delivery1"),
			//	DeliveryFileName = "temp.txt",
			//	Compression = "None",
			//	ReaderAdapterType = "Edge.Data.Pipeline.JsonDynamicReaderAdapter, Edge.Data.Pipeline",

			//	MappingConfigPath = @"C:\Development\Edge.bi\Files\temp\Mappings\61\Analytics.xml",
			//	SampleFilePath = @"C:\Development\Edge.bi\Files\temp\Mappings\61\AnalyticsSample"
			//};

			//config.Parameters["ChecksumTheshold"] = "0.1";
			//config.Parameters["Sql.TransformCommand"] = "SP_Delivery_Transform_BO_Generic(@DeliveryID:NvarChar,@DeliveryTablePrefix:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,?CommitTableName:NvarChar)";
			//config.Parameters["Sql.StageCommand"] = "SP_Delivery_Rollback_By_DeliveryOutputID_v291(@DeliveryOutputID:NvarChar,@TableName:NvarChar)";
			//config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			//config.Parameters["IgnoreDeliveryJsonErrors"] = true;

			return config;
		}

		private static ServiceConfiguration GetTransformConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(MetricsTransformService).AssemblyQualifiedName,
				Limits = {MaxExecutionTime = new TimeSpan(0, 2, 0, 0)},
				DeliveryID = GetGuidFromString("Delivery2"),
				MappingConfigPath = @"C:\Development\Edge.bi\Files\temp\Mappings\1006\FtpAdvertising.xml",
			};

			// TODO shirat - check if should be a part of configuration class and not parameters
			config.Parameters["ChecksumTheshold"] = "0.1";
			config.Parameters["Sql.TransformCommand"] = "SP_Delivery_Transform_BO_Generic(@DeliveryID:NvarChar,@DeliveryTablePrefix:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,?CommitTableName:NvarChar)";
			config.Parameters["Sql.StageCommand"] = "SP_Delivery_Rollback_By_DeliveryOutputID_v291(@DeliveryOutputID:NvarChar,@TableName:NvarChar)";
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["IdentityInDebug"] = false;

			return config;
		}

		private static ServiceConfiguration GetStagingConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(MetricsStagingService).AssemblyQualifiedName,
				Limits = {MaxExecutionTime = new TimeSpan(0, 1, 0, 0)},
				DeliveryID = GetGuidFromString("Delivery2"),
				MappingConfigPath = @"C:\Development\Edge.bi\Files\temp\Mappings\1006\FtpAdvertising.xml",
			};

			// TODO shirat - check if should be a part of configuration class and not parameters
			config.Parameters["ChecksumTheshold"] = "0.1";
			config.Parameters["Sql.TransformCommand"] = "SP_Delivery_Transform_BO_Generic(@DeliveryID:NvarChar,@DeliveryTablePrefix:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,?CommitTableName:NvarChar)";
			config.Parameters["Sql.StageCommand"] = "SP_Delivery_Rollback_By_DeliveryOutputID_v291(@DeliveryOutputID:NvarChar,@TableName:NvarChar)";
			config.Parameters["Sql.RollbackCommand"] = "SP_Delivery_Stage_BO_Generic(@DeliveryFileName:NvarChar,@CommitTableName:NvarChar,@MeasuresNamesSQL:NvarChar,@MeasuresFieldNamesSQL:NvarChar,@OutputIDsPerSignature:varChar,@DeliveryID:NvarChar)";
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;
			config.Parameters["IdentityInDebug"] = false;
			//config.Parameters["CreateNewEdgeObjects"] = false;

			return config;
		}

		private static DateTimeRange? GetTimePeriod()
		{
			var period = new DateTimeRange
			{
				Start = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.Start, BaseDateTime = DateTime.Now.AddDays(-2) },
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

		#region Helper Functions
		private static Guid GetGuidFromString(string key)
		{
			var md5Hasher = MD5.Create();

			// Convert the input string to a byte array and compute the hash.
			byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(key));
			return new Guid(data);
		}

		private static void Clean(ServiceEnvironment environment)
		{
			// delete service events
			//using (var connection = new SqlConnection(environment.EnvironmentConfiguration.ConnectionString))
			//{
			//	connection.Open();
			//	var command = new SqlCommand("delete from [EdgeSystem].[dbo].ServiceEnvironmentEvent where TimeStarted >= '2013-01-01 00:00:00.000'", connection)
			//	{
			//		CommandType = CommandType.Text
			//	};
			//	command.ExecuteNonQuery();
			//}


			// delete previous delivery tables
			using (var deliveryConnection = new SqlConnection(AppSettings.GetConnectionString(typeof(MetricsDeliveryManager), Consts.ConnectionStrings.Deliveries)))
			{
				var cmd = SqlUtility.CreateCommand("Drop_Delivery_tables", CommandType.StoredProcedure);
				cmd.Parameters.AddWithValue("@TableInitial", "3__");
				cmd.Connection = deliveryConnection;
				deliveryConnection.Open();
				cmd.ExecuteNonQuery();

				cmd = new SqlCommand("DELETE [dbo].[MD_MetricsMetadata]", deliveryConnection);
				cmd.ExecuteNonQuery();
			}
		}
		#endregion
	}
}
