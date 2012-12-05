using Edge.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Edge.UnitTests.Core.Services.Scheduling
{
    /// <summary>
    /// Dummy service for test
    /// </summary>
    public class TestService : Service
    {
        protected override ServiceOutcome DoWork()
        {
            Debug.WriteLine(DateTime.Now + String.Format(": Starting '{0}'", this.Configuration.ServiceName));
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Debug.WriteLine(DateTime.Now + String.Format(": Finishing '{0}'", this.Configuration.ServiceName));

            return ServiceOutcome.Success;
        }
    }
}
