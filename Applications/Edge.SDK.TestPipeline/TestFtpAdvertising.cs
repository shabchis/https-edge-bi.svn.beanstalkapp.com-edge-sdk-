using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
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

namespace Edge.SDK.TestPipeline
{
	public class TestFtpAdvertising : BaseTest
	{
		#region Main
		public static void Test()
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

			Log.Write("TestPipeline", "Starting Pipeline Test", LogMessageType.Debug);

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

			var profile = new ServiceProfile { Name = "PipelineProfile" };
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

		private static ServiceConfiguration CreateBaseWorkflow()
		{
			var workflowConfig = new WorkflowServiceConfiguration
			{
				ServiceName = "PipelineWorkflow",
				Workflow = new WorkflowNodeGroup
				{
					Mode = WorkflowNodeGroupMode.Linear,
					Nodes = new LockableList<WorkflowNode>
								{
									//new WorkflowStep {Name = "PipelileTestInitializer", ServiceConfiguration = GetInitializerConfig()},
									//new WorkflowStep {Name = "PipelileTestRetriever", ServiceConfiguration = GetRetrieverConfig()},
									new WorkflowStep {Name = "PipelileTestProcessor", ServiceConfiguration = GetProcessorConfig()},
									new WorkflowStep {Name = "PipelileTestTrasform", ServiceConfiguration = GetTransformConfig()},
									new WorkflowStep {Name = "PipelileTestStaging", ServiceConfiguration = GetStagingConfig()},
								}
				}
			};

			return workflowConfig;
		}

		private static ServiceConfiguration GetInitializerConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(UrlInitializerService).AssemblyQualifiedName,
				DeliveryID = GetGuidFromString("Delivery1"),
				TimePeriod = GetTimePeriod()
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;

			return config;
		}

		private static ServiceConfiguration GetRetrieverConfig()
		{
			var config = new PipelineServiceConfiguration
			{
				ServiceClass = typeof(UrlRetrieverService).AssemblyQualifiedName,
				DeliveryID = GetGuidFromString("Delivery1")
			};
			config.Parameters["IgnoreDeliveryJsonErrors"] = true;

			return config;
		}

		private static ServiceConfiguration GetProcessorConfig()
		{
			//------------------------------------------
			// FtpAdvertesing sample (account 1006)
			//------------------------------------------
			var config = new AutoMetricsProcessorServiceConfiguration
			{
				ServiceClass = typeof(AutoMetricsProcessorService).AssemblyQualifiedName,
				DeliveryID = GetGuidFromString("Delivery1"),
				DeliveryFileName = "temp.txt",
				Compression = "None",
				ReaderAdapterType = "Edge.Data.Pipeline.CsvDynamicReaderAdapter, Edge.Data.Pipeline",

				MappingConfigPath = @"C:\Development\Edge.bi\Files\temp\Mappings\1006\FtpAdvertising.xml",
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
				DeliveryID = GetGuidFromString("Delivery1"),
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
				DeliveryID = GetGuidFromString("Delivery1"),
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
				TimeZone = "UTC",
				Start = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.Start, BaseDateTime = DateTime.Now },
				End = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.Start, BaseDateTime = DateTime.Now.AddHours(2) }
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
