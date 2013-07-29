using Edge.Data.Pipeline.Metrics.Indentity;
namespace Edge.SDK.TestPipeline
{
	class Program
	{
		static void Main()
		{
			//TestEasyForexBackoffice.Test();

			//TestFacebook.Test();

			//TestFtpAdvertising.Test();

			//TestGoogleAdWords.Test();
			
			//TestGoogleAdwordsSettings.Test();

			//TestObjectsUpdate.Test();

			TestGoogleAdwordsGeo.Test();
		}

		private void ManulaTests()
		{
			// test stage data 
			//using (var connection = new SqlConnection(AppSettings.GetConnectionString(typeof(MetricsDeliveryManager), Consts.ConnectionStrings.Objects)))
			//{
			//	connection.Open();
			//	Data.Pipeline.Metrics.Indentity.EdgeViewer.StageMetrics(7, "[DBO].[7__20130703_181234_b1f321e43dd65129b514c55c124466f0_Metrics]", "aaa", connection);
			//}
			//throw new Exception();
			// testing objects viewer
			//using (var connection = new SqlConnection(AppSettings.GetConnectionString(typeof(MetricsDeliveryManager), Consts.ConnectionStrings.Objects)))
			//{
			//	connection.Open();
			//	Data.Pipeline.Metrics.Indentity.EdgeViewer.GetObjectsView(7, "7_Metrics_Search", connection, null);
			//}
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

		}
	}
}
