using System;
using System.Data;
using System.Data.SqlClient;
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
		#region Account Consts
		// --> EasyForex
		protected const int ACCOUNT_ID = 7;
		protected const int CHANNEL_ID = 1;
		protected const string ADWORDS_MCC_EMAIL = "ppc.easynet@gmail.com";
		protected const string ADWORDS_CLIENT_ID = "323-509-6780";
		protected const string FACEBOOK_ACCOUNT_ID = "52081533";
		protected const string FACEBOOK_ACCESS_TOKEN = "CAACZAMUPZCAd0BAC8C5u6ncZCQ9Q6VoBuxfkfocHlvM8fdbn5IDX90YXTaRKaW0IcgyiAZA3CqV80ELmpLZCGZCBfNlj36oSotTvjBw5r6lbXfU8UzawsRDi83UCZAAZClZAgGbP8X9qP86CiZCzeNh10D";   

		// --> Payoneer
		//protected const int ACCOUNT_ID = 1240244;
		//protected const int CHANNEL_ID = 1;
		//protected const string ADWORDS_MCC_EMAIL = "ppc.easynet@gmail.com";
		//protected const string ADWORDS_CLIENT_ID = "272-752-0560";
		//protected const string FACEBOOK_ACCOUNT_ID = "108633745955980";
		//protected const string FACEBOOK_ACCESS_TOKEN = "CAACZAMUPZCAd0BAHeQQaHBEacnnXpQVpBNO2heZB6853BmOQiARSv0NQuA4GZCYcBquKqMUP6jq5XftFdGQqK358ELdARsZC9UAzLyW00GOxZBs7U9xjEKSE4nnrjPFZCZBrEe2YQD84vHOnCmgaZA8Vv";   

		// --> GreenSQL
		//protected const int ACCOUNT_ID = 1240250;
		//protected const int CHANNEL_ID = 3;
		//protected const string ADWORDS_MCC_EMAIL = "ppc.easynet@gmail.com";
		//protected const string ADWORDS_CLIENT_ID = "323-509-6780";
		//protected const string FACEBOOK_ACCOUNT_ID = "52081533";
		//protected const string FACEBOOK_ACCESS_TOKEN = "CAACZAMUPZCAd0BAC8C5u6ncZCQ9Q6VoBuxfkfocHlvM8fdbn5IDX90YXTaRKaW0IcgyiAZA3CqV80ELmpLZCGZCBfNlj36oSotTvjBw5r6lbXfU8UzawsRDi83UCZAAZClZAgGbP8X9qP86CiZCzeNh10D";
		#endregion

		#region Helper Functions
		
		protected void Init(ServiceConfiguration workflowConfig)
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

		protected ServiceEnvironment CreateEnvironment()
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

		protected ServiceConfiguration CreatePipelineWorkflow(ServiceConfiguration workflowConfig)
		{
			var profile = new ServiceProfile { Name = "Profile" };
			profile.Parameters["AccountID"] = ACCOUNT_ID;
			profile.Parameters["ChannelID"] = CHANNEL_ID;
			profile.Parameters["FileDirectory"] = GetTestName();
			profile.Parameters["DeliveryFileName"] = "temp.txt";
			profile.Parameters["SourceUrl"] = "http://google.com";
			profile.Parameters["UsePassive"] = true;
			profile.Parameters["UseBinary"] = false;
			profile.Parameters["UserID"] = "edgedev";
			profile.Parameters["Password"] = "6719AEDC8CD5CC31B9931A7B0CEE1FF7";

			return profile.DeriveConfiguration(workflowConfig);
		}
		
		protected Guid GetGuidFromString(string key)
		{
			var md5Hasher = MD5.Create();

			// Convert the input string to a byte array and compute the hash.
			byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(key));
			return new Guid(data);
		}

		protected void CleanEnv(ServiceEnvironment environment)
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

		protected void CleanDelivery()
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

		protected Guid GetDeliveryId(string serviceName = "")
		{
			return GetGuidFromString(String.Format("Delivery-{0}-{1}", ACCOUNT_ID, serviceName));
		}

		protected string GetTestName()
		{
			return GetType().Name.Replace("Test", "");
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
			Console.WriteLine("{3} ({4}) -- state: {0}, progress: {1}, outcome: {2}", instance.State, instance.Progress.ToString("0.00"), instance.Outcome, instance.Configuration.ServiceName, instance.InstanceID.ToString("N").Substring(0, 4));
		}

		static void instance_OutputGenerated(object sender, ServiceOutputEventArgs e)
		{
			Console.WriteLine("     --> " + e.Output);
		}
		#endregion
	}
}
