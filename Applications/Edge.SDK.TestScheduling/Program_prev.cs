using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Edge.Core.Scheduling;
using Edge.Core.Services;
using Edge.Core.Services.Scheduling;
using Edge.Core.Utilities;

namespace Edge.SDK.TestScheduling
{
    class Program_prev
    {
        static void Main1(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            Log.Write("Test scheduling", "Current time=" + DateTime.Now, LogMessageType.Debug);
            Log.Write("Test scheduling", "Test 1234", LogMessageType.Debug);

            // create service env
            var envConfig = new ServiceEnvironmentConfiguration()
            {
                DefaultHostName = "Johnny",
                ConnectionString = "Data Source=bi_rnd;Initial Catalog=EdgeSystem;Integrated Security=true",
                SP_HostRegister = "Service_HostRegister",
                SP_HostUnregister = "Service_HostUnregister",
                SP_InstanceSave = "Service_InstanceSave",
                SP_InstanceGet = "Service_InstanceGet",
                SP_InstanceReset = "Service_InstanceReset",
                SP_ServicesExecutionStatistics = "Service_ExecutionStatistics_GetByPercentile",
                SP_InstanceActiveListGet = "Service_InstanceActiveList_GetByTime"
            };
            var environment = ServiceEnvironment.Open(envConfig);

            // generate scheduler config, create scheduler and start it
            var schedulerConfig = GenerateConfigForScheduler();
            var scheduler = new Scheduler(environment, schedulerConfig);
            scheduler.Start();

            do
            {
            } while (Console.ReadLine() != "exit");

            scheduler.Stop();
        }

        private static SchedulerConfiguration GenerateConfigForScheduler()
        {
            var schedulerConfig = new SchedulerConfiguration
            {
                Percentile = 80,
                Timeframe = new TimeSpan(2, 0, 0),
                SamplingInterval = new TimeSpan(0, 10, 0),
                ResheduleInterval = new TimeSpan(0, 0, 1),
                ExecutionStatisticsRefreshInterval = new TimeSpan(23, 59, 0)
            };

            // create service configuration
            var serviceList = new List<ServiceConfiguration> 
            {
                new ServiceConfiguration()
                {
                    IsEnabled = true,
                    ServiceName = "Dummy",
                    ServiceClass = typeof(DummyService).AssemblyQualifiedName,
                    HostName = "Johnny"
                }
            };

            // add scheduling rule to service config
            serviceList[0].SchedulingRules.Add(new SchedulingRule
            {
                Scope = SchedulingScope.Day,
                Times = new TimeSpan[] { new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second) },
                MaxDeviationBefore = new TimeSpan(0, 0, 0),
                MaxDeviationAfter = new TimeSpan(0, 10, 0)
            });
            // add service limits
            serviceList[0].Limits.MaxConcurrentPerTemplate = 1;
            serviceList[0].Limits.MaxConcurrentPerProfile = 1;

            // create dummy profiles
            var profileCollection = new ProfilesCollection();

            var profile = new ServiceProfile { Name = "profile1" };
            profile.Parameters["AccountID"] = 1;
            profile.Services.Add(profile.DeriveConfiguration(serviceList[0]));
            profileCollection.Add(profile);

            profile = new ServiceProfile { Name = "profile2" };
            profile.Parameters["AccountID"] = 2;
            profile.Services.Add(profile.DeriveConfiguration(serviceList[0]));
            profile.Services[0].SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            profile.Services[0].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 2, 0);
            profileCollection.Add(profile);

            profile = new ServiceProfile { Name = "profile3" };
            profile.Parameters["AccountID"] = 3;
            profile.Services.Add(profile.DeriveConfiguration(serviceList[0]));
            profile.Services[0].SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            profile.Services[0].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 5, 0);
            profileCollection.Add(profile);

            schedulerConfig.Profiles = profileCollection;
            schedulerConfig.ServiceConfigurationList = serviceList;

            return schedulerConfig;
        }

        private static void TestGuidFromString()
        {
            MD5 md5Hasher = MD5.Create();

            string str = "shira";
            string str1 = "shira1";
            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(str));
            var guid = new Guid(data);

            data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(str1));
            var guid1 = new Guid(data);

            data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(str));
            var guidSame = new Guid(data);
        }
    }

    /// <summary>
    /// Dummy service for test
    /// </summary>
    public class DummyService : Service
    {
        protected override ServiceOutcome DoWork()
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            return ServiceOutcome.Success;
        }
    }
}
