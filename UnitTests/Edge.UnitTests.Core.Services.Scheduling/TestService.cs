using Edge.Core.Services;
using System;
using System.Diagnostics;
using System.Threading;
using Edge.Core.Utilities;

namespace Edge.UnitTests.Core.Services.Scheduling
{
    /// <summary>
    /// Dummy service for test
    /// </summary>
    public class TestService : Service
    {
        protected override ServiceOutcome DoWork()
        {
	        var config = Configuration.GetProfileConfiguration();
	        var profileName = (config != null && config.Profile != null) ? config.Profile.Name : String.Empty;

            WriteLog(String.Format("Start service {0}, profile {1}", Configuration.ServiceName, profileName));

	        for (int i = 0; i < 10; i++)
	        {
				Thread.Sleep(TimeSpan.FromSeconds(1));
		        Progress = (double)i/10;
	        }

			WriteLog(String.Format("Finish service {0}, profile {1}", Configuration.ServiceName, profileName));

            return ServiceOutcome.Success;
        }

		private void WriteLog(string msg)
		{
			Debug.WriteLine("{0}: {1}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), msg);
            Log(msg, LogMessageType.Debug);
		}

    }
}
