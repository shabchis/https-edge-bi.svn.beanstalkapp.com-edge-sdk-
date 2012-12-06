using System;
using Edge.Core;
using Edge.Core.Services.Workflow;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Edge.Core.Services.Scheduling;
using System.Diagnostics;
using System.Collections.Generic;
using Edge.Core.Services;
using Edge.Core.Scheduling;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using Edge.Core.Utilities;

namespace Edge.UnitTests.Core.Services.Scheduling
{
    /// <summary>
    /// Testing for Scheduler component
    /// </summary>
    [TestClass, System.Runtime.InteropServices.GuidAttribute("963D4B5C-D429-416F-8588-40D606AC0942")]
    public class SchedulerTest
    {
        public void TestSetup()
        {
            log4net.Config.XmlConfigurator.Configure();
        } 

        /// <summary>
        /// Test scheduler initialization: load config, load execution statistics, load services for recovery
        /// </summary>
        [TestMethod]
        public void TestInit()
        {
            TestSetup();

            Debug.WriteLine("Current time=" + DateTime.Now);
            
            var env = CreateEnvironment();
            var schedulerConfig = GenerateBaseSchedulerConfig();

            #region Config - service outside the timeframe

            // specific time in day
            var serviceConfig = CreateServiceConfig("service1", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour + schedulerConfig.Timeframe.Hours + 1, 0, 0);
            CreateProfile(1, serviceConfig, schedulerConfig);

            // specific day in the week
            serviceConfig = CreateServiceConfig("service2", schedulerConfig);
            serviceConfig.SchedulingRules[0].Scope = SchedulingScope.Week;
            serviceConfig.SchedulingRules[0].Days = new[] { (int)DateTime.Now.DayOfWeek + 1 };
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour + schedulerConfig.Timeframe.Hours - 1, 0, 0);
            CreateProfile(2, serviceConfig, schedulerConfig);

            // specific day in the month
            serviceConfig = CreateServiceConfig("service3", schedulerConfig);
            serviceConfig.SchedulingRules[0].Scope = SchedulingScope.Month;
            serviceConfig.SchedulingRules[0].Days = new[] { DateTime.Now.Day + 1 };
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour + schedulerConfig.Timeframe.Hours - 1, 0, 0);
            CreateProfile(3, serviceConfig, schedulerConfig);

            PrintConfig(schedulerConfig);
            #endregion

            var scheduler = new Scheduler(env, schedulerConfig);

            // check env and config are set
            Assert.IsNotNull(scheduler.Environment);
            Assert.IsNotNull(scheduler.Configuration);

            // check statistics was laoded
            Assert.IsTrue(scheduler.ServicesExecutionStatisticsDict.Count > 0);
            
            // check profile and service configuration was set properly
            Assert.IsTrue(scheduler.Profiles.Count == 3);
            Assert.IsTrue(scheduler.ServiceConfigurations.Count == 3);

            Assert.IsTrue(scheduler.ScheduledServices.Count == 0);
            Assert.IsTrue(scheduler.UnscheduledServices.Count == 0);
        }

        /// <summary>
        /// Test service outside the timeframe
        /// </summary>
        [TestMethod]
        public void TestNotInTimeframe()
        {
            Debug.WriteLine("Current time=" + DateTime.Now);

            var env = CreateEnvironment();
            var schedulerConfig = GenerateBaseSchedulerConfig();

            #region Config - service outside the timeframe

            // specific time in day
            var serviceConfig = CreateServiceConfig("service1", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour + schedulerConfig.Timeframe.Hours + 1, 0, 0);
            CreateProfile(1, serviceConfig, schedulerConfig);

            // specific day in the week
            serviceConfig = CreateServiceConfig("service2", schedulerConfig);
            serviceConfig.SchedulingRules[0].Scope = SchedulingScope.Week;
            serviceConfig.SchedulingRules[0].Days = new[] { (int)DateTime.Now.DayOfWeek + 1 };
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour + schedulerConfig.Timeframe.Hours - 1, 0, 0);
            CreateProfile(2, serviceConfig, schedulerConfig);

            // specific day in the month
            serviceConfig = CreateServiceConfig("service3", schedulerConfig);
            serviceConfig.SchedulingRules[0].Scope = SchedulingScope.Month;
            serviceConfig.SchedulingRules[0].Days = new[] { DateTime.Now.Day + 1 };
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour + schedulerConfig.Timeframe.Hours - 1, 0, 0);
            CreateProfile(3, serviceConfig, schedulerConfig);

            PrintConfig(schedulerConfig);
            #endregion

            var scheduler = new Scheduler(env, schedulerConfig);
            scheduler.Schedule();

            // assert if was inserted into scheduled or unscheduled
            Assert.IsTrue(scheduler.ScheduledServices.Count == 0);
            Assert.IsTrue(scheduler.UnscheduledServices.Count == 0);
        }

        /// <summary>
        /// Test service inside the timeframe
        /// </summary>
        [TestMethod]
        public void TestInTimeframe()
        {
            Debug.WriteLine("Current time=" + DateTime.Now);

            var env = CreateEnvironment();
            var schedulerConfig = GenerateBaseSchedulerConfig();

            #region Config

            // specific time in day
            var serviceConfig = CreateServiceConfig("service1", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            CreateProfile(1, serviceConfig, schedulerConfig);

            // specific day in the week
            serviceConfig = CreateServiceConfig("service2", schedulerConfig);
            serviceConfig.SchedulingRules[0].Scope = SchedulingScope.Week;
            serviceConfig.SchedulingRules[0].Days[0] = (int)DateTime.Now.DayOfWeek;
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            CreateProfile(2, serviceConfig, schedulerConfig);

            // specific day in the month
            serviceConfig = CreateServiceConfig("service3", schedulerConfig);
            serviceConfig.SchedulingRules[0].Scope = SchedulingScope.Month;
            serviceConfig.SchedulingRules[0].Days[0] = DateTime.Now.Day;
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            CreateProfile(3, serviceConfig, schedulerConfig);

            PrintConfig(schedulerConfig);
            #endregion

            var scheduler = new Scheduler(env, schedulerConfig);
            scheduler.Schedule();

            // 3 services should be inserted into scheduled list
            Assert.IsTrue(scheduler.ScheduledServices.Count == 3);
            Assert.IsTrue(scheduler.UnscheduledServices.Count == 0);

            FinishTest();
        }

        /// <summary>
        /// Test service max deviation
        /// </summary>
        [TestMethod]
        public void TestMaxDeviation()
        {
            Debug.WriteLine("Current time=" + DateTime.Now);

            var env = CreateEnvironment();
            var schedulerConfig = GenerateBaseSchedulerConfig();

            #region Config

            var deviationTime = DateTime.Now.AddMinutes(-15);

            // shouldn't run according to max deviation
            var serviceConfig = CreateServiceConfig("service1", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(deviationTime.Hour, deviationTime.Minute, deviationTime.Second);
            serviceConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 10, 0);
            CreateProfile(1, serviceConfig, schedulerConfig);

            // should run according to max deviation
            serviceConfig = CreateServiceConfig("service2", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(deviationTime.Hour, deviationTime.Minute, deviationTime.Second);
            serviceConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 20, 0);
            CreateProfile(2, serviceConfig, schedulerConfig);

            PrintConfig(schedulerConfig);

            #endregion

            var scheduler = new Scheduler(env, schedulerConfig);
            scheduler.Schedule();

            // only service2 should be scheduled
            Assert.IsTrue(scheduler.ScheduledServices.Count == 1);
            Assert.IsTrue(scheduler.UnscheduledServices.Count == 0);
            Assert.IsTrue(scheduler.ScheduledServices[0].Configuration.ServiceName == "service2");
        }

        /// <summary>
        /// TODO: Test service that can not be scheduled
        /// </summary>
        [TestMethod]
        public void TestCouldNotBeScheduled()
        {
            Debug.WriteLine("Current time=" + DateTime.Now);

            var env = CreateEnvironment();
            var schedulerConfig = GenerateBaseSchedulerConfig();

            #region Config - service outside the timeframe

            // shouldn't run according to max deviation
            var serviceConfig = CreateServiceConfig("service1", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            serviceConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 1, 0);
            CreateProfile(1, serviceConfig, schedulerConfig);

            // should run according to max deviation
            serviceConfig = CreateServiceConfig("service2", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            serviceConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 10, 0);
            CreateProfile(2, serviceConfig, schedulerConfig);

            PrintConfig(schedulerConfig);
            #endregion

            var scheduler = new Scheduler(env, schedulerConfig);
            scheduler.Schedule();

            // two services are scheduled
            Assert.IsTrue(scheduler.ScheduledServices.Count == 2 && scheduler.UnscheduledServices.Count == 0);

            // ?????????????????????????
            Thread.Sleep(new TimeSpan(0, 1, 0));
            scheduler.Schedule();

            Assert.IsTrue(scheduler.ScheduledServices.Count == 1);
            Assert.IsTrue(scheduler.UnscheduledServices.Count == 0);
        }

        /// <summary>
        /// Test concurrent per template services
        /// </summary>
        [TestMethod]
        public void TestConcurrentPerTemplate()
        {
            Debug.WriteLine("Current time=" + DateTime.Now);

            var env = CreateEnvironment();
            var schedulerConfig = GenerateBaseSchedulerConfig();

            #region Config

            // base service
            var serviceConfig = CreateServiceConfig("service1", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            
            // the same service for 3 profiles
            var profile1 = CreateProfile(1, serviceConfig, schedulerConfig);
            CreateProfile(2, serviceConfig, schedulerConfig);
            var profile3 = CreateProfile(3, serviceConfig, schedulerConfig);
            profile3.Services[0].GetProfileConfiguration().SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 2, 0);

            // add another service for profile1
            serviceConfig = CreateServiceConfig("service2", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            profile1.Services.Add(GetProfileConfiguration(serviceConfig, profile1));

            PrintConfig(schedulerConfig);
            #endregion

            var scheduler = new Scheduler(env, schedulerConfig);
            scheduler.Schedule();

            // two services are scheduled
            Assert.IsTrue(scheduler.ScheduledServices.Count == 3);
            
            // service1 for profile1 and service2 for profile1 can be scheduled at the same time
            Assert.IsTrue(scheduler.ScheduledServices[0].SchedulingInfo.ExpectedStartTime.RemoveMilliseconds() == 
                          scheduler.ScheduledServices[1].SchedulingInfo.ExpectedStartTime.RemoveMilliseconds());

            // service1 for profile1 is scheduled after service1 for profile2 - not at the same time
            Assert.IsTrue(scheduler.ScheduledServices[2].SchedulingInfo.ExpectedStartTime.RemoveMilliseconds() > 
                          scheduler.ScheduledServices[0].SchedulingInfo.ExpectedStartTime.RemoveMilliseconds());

            // the service1 for profile1 is not scheduled - ???
            Assert.IsTrue(scheduler.UnscheduledServices.Count == 1);
            Assert.IsTrue(scheduler.UnscheduledServices[0].Configuration.Profile.Name == "profile3");
        }

        /// <summary>
        /// Test concurrent per template services + max deviation after
        /// </summary>
        [TestMethod]
        public void TestConcurrentPerTemplateAndDeviationAfter()
        {
            Debug.WriteLine("Current time=" + DateTime.Now);

            var env = CreateEnvironment();
            var schedulerConfig = GenerateBaseSchedulerConfig();

            #region Config

            // last hour and default deviation = 3 hours
            var serviceConfig = CreateServiceConfig("service1", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour - 1, DateTime.Now.Minute, DateTime.Now.Second);

            // the same service for another profile and deviation 10 sec
            CreateProfile(1, serviceConfig, schedulerConfig);
            var profile2 = CreateProfile(2, serviceConfig, schedulerConfig);
            profile2.Services[0].GetProfileConfiguration().SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            profile2.Services[0].GetProfileConfiguration().SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 1, 0);

            PrintConfig(schedulerConfig);
            #endregion

            var scheduler = new Scheduler(env, schedulerConfig);
            scheduler.Schedule();

            // two services are scheduled
            Assert.IsTrue(scheduler.ScheduledServices.Count == 2);

            // service1 for profile2 is scheduled before service1 for profile1 - not at the same time
            Assert.IsTrue(scheduler.ScheduledServices[0].SchedulingInfo.ExpectedStartTime.RemoveMilliseconds() >
                          scheduler.ScheduledServices[1].SchedulingInfo.ExpectedStartTime.RemoveMilliseconds());

            Assert.IsTrue(scheduler.ScheduledServices[0].Configuration.Profile.Name == "profile2");
            Assert.IsTrue(scheduler.ScheduledServices[1].Configuration.Profile.Name == "profile1");

            FinishTest();
        }

        /// <summary>
        /// TODO: Test concurrent per profile services
        /// </summary>
        [TestMethod]
        public void TestConcurrentPerProfile()
        {
            Debug.WriteLine("Current time=" + DateTime.Now);

            var env = CreateEnvironment();
            var schedulerConfig = GenerateBaseSchedulerConfig();

            #region Config

            // service with max per template = 5
            var serviceConfig = CreateServiceConfig("service1", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour + 1, 0, 0);
            
            serviceConfig.Limits.MaxConcurrentPerTemplate = 5;
            serviceConfig.Limits.MaxConcurrentPerProfile = 1;

            // the same service for 2 profiles
            var profile1 = CreateProfile(1, serviceConfig, schedulerConfig);
            var profile2 = CreateProfile(2, serviceConfig, schedulerConfig);

            // add additional rule to the profile1
            profile1.Services[0].SchedulingRules.Add(new SchedulingRule
            {
                Scope = SchedulingScope.Week,
                Days = new[] { (int)DateTime.Now.DayOfWeek },
                Times = new[] { new TimeSpan(DateTime.Now.Hour + 1, 0, 0) },
            });

            // same service for the same profile
            //profile1.Services.Add(profile1.DeriveConfiguration(serviceConfig));
            
            PrintConfig(schedulerConfig);
            #endregion

            var scheduler = new Scheduler(env, schedulerConfig);
            scheduler.Schedule();

            // two services are scheduled
            Assert.IsTrue(scheduler.ScheduledServices.Count == 3);

            // service1 for profile1 and service2 for profile1 can be scheduled at the same time
            Assert.IsTrue(scheduler.ScheduledServices[0].SchedulingInfo.ExpectedStartTime.RemoveMilliseconds() == 
                          scheduler.ScheduledServices[2].SchedulingInfo.ExpectedStartTime.RemoveMilliseconds());

            // service1 for profile1 cannot be scheduled at the same time
            Assert.IsTrue(scheduler.ScheduledServices[0].SchedulingInfo.ExpectedStartTime.RemoveMilliseconds() !=
                          scheduler.ScheduledServices[1].SchedulingInfo.ExpectedStartTime.RemoveMilliseconds());
        }

        #region Recovery Tests
        /// <summary>
        /// Test recovery: already running services or ended but can be scheduled again
        /// this test should be run twice: 1st run to run services, 2nd time - for recovery
        /// Note to clean ServiceInstance table before the 1st test but not to clean after the 1st test
        /// TODO: set base line time close to now, but do not change for the 2nd run
        ///       wait 1 min between 2 runs
        ///       change is isFirstTime between the runs accordingly
        /// </summary>
        [TestMethod]
        public void TestRecovery()
        {
            Debug.WriteLine("Current time=" + DateTime.Now);
            var baseLineTme = new DateTime(2012, 12, 4, 11, 44, 0);

            // 1st run - all services are shceduled and executed
            TestRun(false, baseLineTme);

            //Thread.Sleep(11000);

            // 2nd run - no services are executed
            //TestRun(false, baseLineTme);
        }

        private void TestRun(bool isFirstRun, DateTime serviceTime)
        {
            var env = CreateEnvironment(isFirstRun);
            var schedulerConfig = GenerateBaseSchedulerConfig();

            #region Config

            // short deviation after = 10 sec
            var serviceConfig = CreateServiceConfig("service1", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(serviceTime.Hour, serviceTime.Minute, serviceTime.Second);
            serviceConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 1, 0);
            CreateProfile(1, serviceConfig, schedulerConfig);

            // deviation after = 30 min
            serviceConfig = CreateServiceConfig("service2", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(serviceTime.Hour, serviceTime.Minute, serviceTime.Second);
            serviceConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);
            CreateProfile(2, serviceConfig, schedulerConfig);

            // default deviation = 3 hours
            serviceConfig = CreateServiceConfig("service3", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(serviceTime.Hour, serviceTime.Minute, serviceTime.Second);
            CreateProfile(3, serviceConfig, schedulerConfig);

            PrintConfig(schedulerConfig);
            #endregion

            var scheduler = new Scheduler(env, schedulerConfig);

            // 1st run: no services from recovery
            // 2nd run: 2 service were loaded into scheduled list from recovery
            Assert.IsTrue(scheduler.ScheduledServices.Count == (isFirstRun ? 0 : 2));
            Assert.IsTrue(scheduler.UnscheduledServices.Count == 0);

            scheduler.Schedule();

            // 1st run: all services should be inserted into scheduled list
            // 2nd run: no services inserted because of already existing services from recovery
            Assert.IsTrue(scheduler.ScheduledServices.Count == (isFirstRun ? 3 : 2));
            Assert.IsTrue(scheduler.UnscheduledServices.Count == 0);

            FinishTest();
        } 
        #endregion

        /// <summary>
        /// Test service inside the timeframe
        /// </summary>
        [TestMethod]
        public void TestRemoveEndedServices()
        {
            Debug.WriteLine("Current time=" + DateTime.Now);

            var env = CreateEnvironment();
            var schedulerConfig = GenerateBaseSchedulerConfig();

            #region Config

            // schedule now with default max deviation = 3 hours
            var serviceConfig = CreateServiceConfig("service1", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            CreateProfile(1, serviceConfig, schedulerConfig);

            // schedule now with max deviation = 10 sec
            serviceConfig = CreateServiceConfig("service2", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            serviceConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 0, 15);
            CreateProfile(2, serviceConfig, schedulerConfig);

            PrintConfig(schedulerConfig);
            #endregion

            var scheduler = new Scheduler(env, schedulerConfig);
            scheduler.Schedule();

            // 2 services should be inserted into scheduled list
            Assert.IsTrue(scheduler.ScheduledServices.Count == 2);
            Assert.IsTrue(scheduler.UnscheduledServices.Count == 0);

            // wait 12 sec
            Thread.Sleep(16000);

            // schedule again
            scheduler.Schedule();

            // only 1st service remain in scheduled the 2nd one removed and no more new services were scheduled
            Assert.IsTrue(scheduler.ScheduledServices.Count == 1);
            Assert.IsTrue(scheduler.ScheduledServices[0].Configuration.ServiceName == "service1");
            Assert.IsTrue(scheduler.UnscheduledServices.Count == 0);

            FinishTest();
        }

        /// <summary>
        /// TODO: Test adding unplaned service
        /// </summary>
        [TestMethod]
        public void TestAddUnplannedService()
        { }

        /// <summary>
        /// Test long running services. 2 options:
        /// 1st - cannot be scheduled --> set max deviation for service1
        /// 2nd - sheduled only after the 1st service finished - remark max deviation for service1
        /// </summary>
        [TestMethod]
        public void TestLongRunningServices()
        {
            Debug.WriteLine("Current time=" + DateTime.Now);

            var env = CreateEnvironment();
            var schedulerConfig = GenerateBaseSchedulerConfig();

            #region Config

            // schedule now
            var serviceConfig = CreateServiceConfig("service1", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            serviceConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 1, 30);
            CreateProfile(1, serviceConfig, schedulerConfig);

            // schedule now the same service for another profile with default max deviation = 3 hours
            CreateProfile(2, serviceConfig, schedulerConfig);

            PrintConfig(schedulerConfig);
            #endregion

            var scheduler = new Scheduler(env, schedulerConfig);
            scheduler.Schedule();

            // 2 services should be scheduled but not at the same time
            Assert.IsTrue(scheduler.ScheduledServices.Count == 2);
            Assert.IsTrue(scheduler.ScheduledServices[1].SchedulingInfo.ExpectedStartTime.RemoveMilliseconds() > 
                          scheduler.ScheduledServices[0].SchedulingInfo.ExpectedStartTime.RemoveMilliseconds());

            // till both services are not ended, reschedule every 0.5 min
            while (scheduler.ScheduledServices[0].State != ServiceState.Ended &&
                   scheduler.ScheduledServices[1].State != ServiceState.Ended)
            {
                Thread.Sleep(30000);
                scheduler.Schedule();

                // what asserts to do???
            }
        }

        /// <summary>
        /// Test workflow service scheduling
        /// </summary>
        [TestMethod]
        public void TestWorkflowServices()
        {
            Debug.WriteLine(DateTime.Now + ": Start workflow test");

            var env = CreateEnvironment();
            var schedulerConfig = GenerateBaseSchedulerConfig();

            #region Config

            // workflow step definition
            var stepConfig = new ServiceConfiguration
            {
                ServiceClass = typeof(TestService).AssemblyQualifiedName
            };

            // workflow definition
            var workflowConfig = new WorkflowServiceConfiguration { ServiceName = "workflowService" };
            workflowConfig.Workflow = new WorkflowNodeGroup
            {
                Mode = WorkflowNodeGroupMode.Linear,
                Nodes = new LockableList<WorkflowNode>
				{
					new WorkflowStep { Name = "service1", ServiceConfiguration =  stepConfig},
					new WorkflowStep { Name = "service2", ServiceConfiguration =  stepConfig},
					new WorkflowStep { Name = "service3", ServiceConfiguration =  stepConfig},
					new WorkflowStep { Name = "service4", ServiceConfiguration =  stepConfig},
					new WorkflowStep { Name = "service5", ServiceConfiguration =  stepConfig},
					new WorkflowStep { Name = "service6", ServiceConfiguration =  stepConfig},
					new WorkflowStep { Name = "service7", ServiceConfiguration =  stepConfig},
				}
            };

            // set scheduling for workflow service
            workflowConfig.SchedulingRules.Add(new SchedulingRule
                {
                    Scope = SchedulingScope.Day,
                    Times = new[] { new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second) },
                    Days = new[] { 0 }
                });
            // create profile related to workflow
            CreateProfile(1, workflowConfig, schedulerConfig);

            PrintConfig(schedulerConfig);
            #endregion

            var scheduler = new Scheduler(env, schedulerConfig);
            scheduler.Start();

            // till workflow is not ended
            while (scheduler.ScheduledServices.Count == 0 || scheduler.ScheduledServices[0].State != ServiceState.Ended)
            {
                Thread.Sleep(5000);
            }

            Debug.WriteLine(DateTime.Now + ": Finishing workflow test");
            Thread.Sleep(120000);
            Debug.WriteLine(DateTime.Now + ": Finishing workflow test");
        }

        #region Integration Test
        /// <summary>
        /// TODO: full integration test with scenario including Scheduler.Start(), Scheduler.Stop(), Scheduler.AddRequest()
        /// </summary>
        [TestMethod]
        public void TestFullIntegration()
        {
            Debug.WriteLine(DateTime.Now + ": Start integration test");

            var env = CreateEnvironment();
            var schedulerConfig = GenerateBaseSchedulerConfig();

            GetIntegrationTestConfig(schedulerConfig);

            var scheduler = new Scheduler(env, schedulerConfig);
            scheduler.Start();

            while (true)
            {
                Thread.Sleep(60000);
            }
        }

        private void GetIntegrationTestConfig(SchedulerConfiguration schedulerConfig)
        {
            // every day on 15:00, deviation 1 min
            var serviceConfig = CreateServiceConfig("service1", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(10, 24, 0);
            serviceConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 1, 0);
            CreateProfile(1, serviceConfig, schedulerConfig);

            // mid week on 15:10
            serviceConfig = CreateServiceConfig("service2", schedulerConfig);
            serviceConfig.SchedulingRules[0].Scope = SchedulingScope.Week;
            serviceConfig.SchedulingRules[0].Days = new[] { 0, 1, 2, 3, 4 };
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(10, 24, 0);
            CreateProfile(2, serviceConfig, schedulerConfig);

            serviceConfig = CreateServiceConfig("service3", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(10, 22, 0);
            CreateProfile(3, serviceConfig, schedulerConfig);

            serviceConfig = CreateServiceConfig("service4", schedulerConfig);
            serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(10, 23, 0);
            CreateProfile(4, serviceConfig, schedulerConfig);

            PrintConfig(schedulerConfig);
        } 
        #endregion

        #region Private Help Functions
        private SchedulerConfiguration GenerateBaseSchedulerConfig()
        {
            // general config
            var schedulerConfig = new SchedulerConfiguration
                {
                    Percentile = 80,
                    MaxExecutionTimeFactor = 2,
                    Timeframe = new TimeSpan(2, 0, 0),
                    SamplingInterval = new TimeSpan(0, 0, 10),
                    ResheduleInterval = new TimeSpan(0, 0, 1),
                    ExecutionStatisticsRefreshInterval = new TimeSpan(0, 1, 0),
                    ServiceConfigurationList = new List<ServiceConfiguration>(),
                    Profiles = new ProfilesCollection()
                };

            return schedulerConfig;
        }

        private ServiceEnvironment CreateEnvironment(bool cleanRecovery = true)
        {
            // create service env
            var envConfig = new ServiceEnvironmentConfiguration
                {
                DefaultHostName = "Johnny",
                ConnectionString = "Data Source=bi_rnd;Initial Catalog=EdgeSystem;Integrated Security=true",
                SP_HostList = "Service_HostList",
                SP_HostRegister = "Service_HostRegister",
                SP_HostUnregister = "Service_HostUnregister",
                SP_InstanceSave = "Service_InstanceSave",
                SP_InstanceGet = "Service_InstanceGet",
                SP_InstanceReset = "Service_InstanceReset",
                SP_EnvironmentEventList = "Service_EnvironmentEventList",
                SP_EnvironmentEventRegister = "Service_EnvironmentEventRegister",
                SP_ServicesExecutionStatistics = "Service_ExecutionStatistics_GetByPercentile",
                SP_InstanceActiveListGet = "Service_InstanceActiveList_GetByTime"
            };

            var environment = new ServiceEnvironment(envConfig);
            var host = new ServiceExecutionHost(environment.EnvironmentConfiguration.DefaultHostName, environment);

            if (cleanRecovery)
            {
                // clean all records from ServiceInstance table
                CleanRecovery(environment);
            }
            return environment;
        }

        private ServiceConfiguration CreateServiceConfig(string serviceName, SchedulerConfiguration schedulerConfig)
        {
            var serviceConfig = new ServiceConfiguration
                {
                    IsEnabled = true,
                    ServiceName = serviceName,
                    ServiceClass = typeof (TestService).AssemblyQualifiedName,
                    HostName = "Johnny",
                    ConfigurationID = GetGuidFromString(serviceName),
                    Limits = {MaxConcurrentPerTemplate = 1, MaxConcurrentPerProfile = 1}
                };

            // add scheduling rule to service config
            serviceConfig.SchedulingRules.Add(new SchedulingRule
            {
                Scope = SchedulingScope.Day,
                Times = new[] { new TimeSpan(0, 0, 0) },
                Days = new[] {0}
            });

            // add to service list in configuration
            schedulerConfig.ServiceConfigurationList.Add(serviceConfig);

            return serviceConfig;
        }

        private ServiceProfile CreateProfile(int accountId, ServiceConfiguration serviceConfig, SchedulerConfiguration schedulerConfig)
        {
            var profile = new ServiceProfile { Name =  String.Format("profile{0}", accountId)};
            profile.Parameters["AccountID"] = accountId;

            profile.Services.Add(GetProfileConfiguration(serviceConfig, profile));
            schedulerConfig.Profiles.Add(profile);

            return profile;
        }

        private ServiceConfiguration GetProfileConfiguration(ServiceConfiguration serviceConfig, ServiceProfile profile)
        {
            var config = profile.DeriveConfiguration(serviceConfig);
            config.ConfigurationID = GetGuidFromString(profile.Name);
            return config;
        }

        private void  PrintConfig(SchedulerConfiguration schedulerConfig)
        {
            foreach(var profile in schedulerConfig.Profiles)
            {
                foreach(var service in profile.Services)
                {
                    foreach(var rule in service.SchedulingRules)
                    {
                        Debug.WriteLine(DateTime.Now + String.Format(": Configuration: Profile {0}, service {1}, rule: scope={2}, time={3}, day={4}, max deviation after={5}, max deviation before={6}", 
                                        profile.Name, service.ServiceName, rule.Scope, rule.Times[0], rule.Days[0],
                                        rule.MaxDeviationAfter, rule.MaxDeviationBefore));

                    }
                }
            }
        }

        private void FinishTest()
        {
            // sleel in test finish in order to enable to all services finish
            Thread.Sleep(10000);
        }

        private Guid GetGuidFromString(string key)
        {
            MD5 md5Hasher = MD5.Create();

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(key));
            return new Guid(data);
        }

        private void CleanRecovery(ServiceEnvironment environment)
        {
            var env = environment.EnvironmentConfiguration;
            using (var connection = new SqlConnection(env.ConnectionString))
            {
                var command = new SqlCommand("delete from dbo.ServiceInstance_v3", connection)
                    {
                        CommandType = CommandType.Text
                    };
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
        #endregion
    }
}
