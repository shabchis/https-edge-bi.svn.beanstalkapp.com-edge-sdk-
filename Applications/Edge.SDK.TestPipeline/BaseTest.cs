using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Edge.Core.Configuration;
using Edge.Core.Services;
using Edge.Core.Utilities;
using Edge.Data.Pipeline.Metrics.Managers;
using Edge.Data.Pipeline.Metrics.Misc;

namespace Edge.SDK.TestPipeline
{
	public abstract class BaseTest
	{
		// --> EasyForex
		//protected const int ACCOUNT_ID = 7;
		//protected const int CHANNEL_ID = 1;
		//protected const string FILE_DIRECTORY = "GoogleAdwords";
		//protected const string ADWORDS_MCC_EMAIL = "ppc.easynet@gmail.com";
		//protected const string ADWORDS_CLIENT_ID = "323-509-6780"; 

		// --> Payoneer
		protected const int ACCOUNT_ID = 1240244;
		protected const int CHANNEL_ID = 1;
		protected const string FILE_DIRECTORY = "PayoneerBackoffice";
		protected const string ADWORDS_MCC_EMAIL = "ppc.easynet@gmail.com";
		protected const string ADWORDS_CLIENT_ID = "272-752-0560";   

		#region Helper Functions
		
		protected static void Init(ServiceConfiguration workflowConfig)
		{
			log4net.Config.XmlConfigurator.Configure();
			Log.Start();

			Log.Write("TestGoogleAdWords", "Starting Google Adwords Test", LogMessageType.Debug);

			var environment = CreateEnvironment();
			CleanEnv(environment);

			var profileServiceConfig = CreatePipelineWorkflow(workflowConfig);

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

		protected static ServiceEnvironment CreateEnvironment()
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

		protected static ServiceConfiguration CreatePipelineWorkflow(ServiceConfiguration workflowConfig)
		{
			var profile = new ServiceProfile { Name = "Profile" };
			profile.Parameters["AccountID"] = ACCOUNT_ID;
			profile.Parameters["ChannelID"] = CHANNEL_ID;
			profile.Parameters["FileDirectory"] = FILE_DIRECTORY;
			profile.Parameters["DeliveryFileName"] = "temp.txt";
			profile.Parameters["SourceUrl"] = "http://google.com";
			profile.Parameters["UsePassive"] = true;
			profile.Parameters["UseBinary"] = false;
			profile.Parameters["UserID"] = "edgedev";
			profile.Parameters["Password"] = "6719AEDC8CD5CC31B9931A7B0CEE1FF7";

			return profile.DeriveConfiguration(workflowConfig);
		}
		
		protected static Guid GetGuidFromString(string key)
		{
			var md5Hasher = MD5.Create();

			// Convert the input string to a byte array and compute the hash.
			byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(key));
			return new Guid(data);
		}

		protected static void CleanEnv(ServiceEnvironment environment)
		{
			// delete service events
			using (var connection = new SqlConnection(environment.EnvironmentConfiguration.ConnectionString))
			{
				connection.Open();
				var command = new SqlCommand("delete from [EdgeSystem].[dbo].ServiceEnvironmentEvent where TimeStarted >= '2013-01-01 00:00:00.000'", connection)
				{
					CommandType = CommandType.Text
				};
				command.ExecuteNonQuery();
			}
		}

		protected static void CleanDelivery()
		{
			// delete previous delivery tables
			using (var deliveryConnection = new SqlConnection(AppSettings.GetConnectionString(typeof(MetricsDeliveryManager), Consts.ConnectionStrings.Deliveries)))
			{
				var cmd = SqlUtility.CreateCommand("Drop_Delivery_tables", CommandType.StoredProcedure);
				cmd.Parameters.AddWithValue("@TableInitial", String.Format("{0}_", ACCOUNT_ID));
				cmd.Connection = deliveryConnection;
				deliveryConnection.Open();
				cmd.ExecuteNonQuery();

				cmd = new SqlCommand("DELETE [dbo].[MD_MetricsMetadata]", deliveryConnection);
				cmd.ExecuteNonQuery();
			}
		}

		protected static Guid GetDeliveryId(string serviceName = "")
		{
			return GetGuidFromString(String.Format("Delivery-{0}-{1}", ACCOUNT_ID, serviceName));
		}
		#endregion

		#region Events
		static void Environment_ServiceRequiresScheduling(object sender, ServiceInstanceEventArgs e)
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
