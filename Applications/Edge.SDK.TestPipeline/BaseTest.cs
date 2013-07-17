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
	public class BaseTest
	{
		#region Helper Functions
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
				cmd.Parameters.AddWithValue("@TableInitial", "7__");
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
