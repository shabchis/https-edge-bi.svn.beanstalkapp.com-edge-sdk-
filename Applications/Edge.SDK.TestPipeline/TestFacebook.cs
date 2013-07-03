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
using Edge.SDK.TestPipeline.Services;
using Edge.Services.Facebook.GraphApi;

namespace Edge.SDK.TestPipeline
{
	public class TestFacebook
	{
		#region Main

		static void Main5()
		{
			log4net.Config.XmlConfigurator.Configure();
			Log.Start();

			Log.Write("TestEasyForexBackoffice", "Starting Easy Forex Backoffice Test", LogMessageType.Debug);

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

			var profile = new ServiceProfile { Name = "EasyForexProfile" };
			profile.Parameters["AccountID"] = 7;
			profile.Parameters["ChannelID"] = 1;
			profile.Parameters["FileDirectory"] = "EasyForex";
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
									//new WorkflowStep {Name = "EasyForexBackofficeInitializer", ServiceConfiguration = GetInitializerConfig()},
									//new WorkflowStep {Name = "EasyForexBackofficeRetriever", ServiceConfiguration = GetRetrieverConfig()},
									new WorkflowStep {Name = "EasyForexBackofficeProcessor", ServiceConfiguration = GetProcessorConfig()},
									new WorkflowStep {Name = "EasyForexBackofficeTrasform", ServiceConfiguration = GetTransformConfig()},
									new WorkflowStep {Name = "EasyForexBackofficeStaging", ServiceConfiguration = GetStagingConfig()},
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
			config.Parameters["IdentityInDebug"] = false;

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
			config.Parameters["IdentityInDebug"] = false;

			return config;
		}

		private static DateTimeRange? GetTimePeriod()
		{
			var period = new DateTimeRange
			{
				Start = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.Start, BaseDateTime = new DateTime(2013, 4, 4, 0, 0, 0) }, //DateTime.Today.AddDays(-1) },
				End = new DateTimeSpecification { Alignment = DateTimeSpecificationAlignment.End, BaseDateTime = new DateTime(2013, 4, 4, 23, 59, 59) }, //DateTime.Today.AddSeconds(-1) }
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
			using (var deliveryConnection = new SqlConnection(AppSettings.GetConnectionString(typeof(MetricsDeliveryManager), Edge.Data.Pipeline.Metrics.Misc.Consts.ConnectionStrings.Deliveries)))
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
