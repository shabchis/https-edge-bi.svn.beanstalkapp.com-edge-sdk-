using System;
using Edge.Core;
using Edge.Core.Services.Workflow;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Edge.Core.Services.Scheduling;
using System.Diagnostics;
using System.Collections.Generic;
using Edge.Core.Services;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using System.Data.SqlClient;
using System.Data;

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

			// special case for bug when service was schadule even not in timeframe:
			// requested time + max deviation >= DateTzime.Now
			serviceConfig = CreateServiceConfig("service4", schedulerConfig);
			serviceConfig.SchedulingRules[0].Scope = SchedulingScope.Week;
			serviceConfig.SchedulingRules[0].Days = new[] { (int)DateTime.Now.DayOfWeek + 1 };
			serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour - 1, 0, 0);
			CreateProfile(4, serviceConfig, schedulerConfig);

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
			schedulerConfig.DefaultExecutionTime = new TimeSpan(0, 0, 30);

			#region Config - service outside the timeframe

			var serviceConfig = CreateServiceConfig("service1", schedulerConfig);
			serviceConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 1, 0);
			serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
			
			CreateProfile(1, serviceConfig, schedulerConfig);
			CreateProfile(2, serviceConfig, schedulerConfig);
			
			PrintConfig(schedulerConfig);
			#endregion

			var scheduler = new Scheduler(env, schedulerConfig);
			scheduler.Schedule();

			// two services are scheduled
			Assert.IsTrue(scheduler.ScheduledServices.Count == 2 && scheduler.UnscheduledServices.Count == 0);

			Thread.Sleep(new TimeSpan(0, 1, 5));
			scheduler.Schedule();

			// service that cannot be scheduled was aborted
			Assert.IsTrue(scheduler.ScheduledServices.Count == 1 && 
						  scheduler.ScheduledServices[0].SchedulingInfo.SchedulingStatus == SchedulingStatus.CouldNotBeScheduled &&
						  scheduler.ScheduledServices[0].Outcome == ServiceOutcome.Canceled);

			scheduler.Schedule();

			// verify the service was removed and not executed
			Assert.IsTrue(scheduler.ScheduledServices.Count == 0);
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

			// base service with max deviation = 5 min
			var serviceConfig = CreateServiceConfig("service1", schedulerConfig);
			serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
			serviceConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 5, 0);
			
			// the same service for 3 profiles
			var profile1 = CreateProfile(1, serviceConfig, schedulerConfig);
			CreateProfile(2, serviceConfig, schedulerConfig);
			CreateProfile(3, serviceConfig, schedulerConfig);
			
			// add another service for profile1
			serviceConfig = CreateServiceConfig("service2", schedulerConfig);
			serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
			profile1.Services.Add(GetProfileConfiguration(serviceConfig, profile1));

			PrintConfig(schedulerConfig);
			#endregion

			var scheduler = new Scheduler(env, schedulerConfig);
			scheduler.Schedule();

			// all services are scheduled
			Assert.IsTrue(scheduler.ScheduledServices.Count == 4);

			Assert.IsTrue(scheduler.ScheduledServices[0].SchedulingInfo.SchedulingStatus == SchedulingStatus.Scheduled);
			Assert.IsTrue(scheduler.ScheduledServices[1].SchedulingInfo.SchedulingStatus == SchedulingStatus.Scheduled);
			Assert.IsTrue(scheduler.ScheduledServices[3].SchedulingInfo.SchedulingStatus == SchedulingStatus.Scheduled);
			
			// service1 for profile3 cannot be schedueld
			Assert.IsTrue(scheduler.ScheduledServices[2].SchedulingInfo.SchedulingStatus == SchedulingStatus.CouldNotBeScheduled);
			Assert.IsTrue(scheduler.ScheduledServices[2].Configuration.ServiceName == "service1");
			Assert.IsTrue(scheduler.ScheduledServices[2].Configuration.Profile.Name == "profile-3");

			// service1 for profile1 and service2 for profile1 can be scheduled at the same time
			Assert.IsTrue(scheduler.ScheduledServices[0].SchedulingInfo.ExpectedStartTime.RemoveMilliseconds() == 
						  scheduler.ScheduledServices[3].SchedulingInfo.ExpectedStartTime.RemoveMilliseconds());

			// service1 for profile1 is scheduled after service1 for profile2 - not at the same time
			Assert.IsTrue(scheduler.ScheduledServices[1].SchedulingInfo.ExpectedStartTime.RemoveMilliseconds() > 
						  scheduler.ScheduledServices[0].SchedulingInfo.ExpectedStartTime.RemoveMilliseconds());
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
		public void TestWorkflowServices(bool startHost = true)
		{
			Debug.WriteLine(DateTime.Now + ": Start workflow test");

			var env = CreateEnvironment(startHost: startHost);
			var schedulerConfig = GenerateBaseSchedulerConfig();

			#region Config

			// workflow step definition
			var stepConfig = new ServiceConfiguration
			{
				ServiceClass = typeof(TestService).AssemblyQualifiedName
			};

			// workflow definition
			var workflowConfig = new WorkflowServiceConfiguration
				{
					ServiceName = "workflowService",
					Workflow = new WorkflowNodeGroup
						{
							Mode = WorkflowNodeGroupMode.Linear,
							Nodes = new LockableList<WorkflowNode>
								{
									new WorkflowStep {Name = "service1", ServiceConfiguration = stepConfig},
									new WorkflowStep {Name = "service2", ServiceConfiguration = stepConfig},
									new WorkflowStep {Name = "service3", ServiceConfiguration = stepConfig},
									new WorkflowStep {Name = "service4", ServiceConfiguration = stepConfig},
									new WorkflowStep {Name = "service5", ServiceConfiguration = stepConfig},
									new WorkflowStep {Name = "service6", ServiceConfiguration = stepConfig},
									new WorkflowStep {Name = "service7", ServiceConfiguration = stepConfig},
								}
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

			Debug.WriteLine(DateTime.Now + ": Finish workflow test");
		}

		/// <summary>
		/// Test for same services for the same profiles for the same time
		/// all services should be scheduled
		/// </summary>
		[TestMethod]
		public void TestSameServices()
		{
			Debug.WriteLine("Current time=" + DateTime.Now);

			var env = CreateEnvironment();
			var schedulerConfig = GenerateBaseSchedulerConfig();

			#region Config - service outside the timeframe

			// create service and profile
			var serviceConfig = CreateServiceConfig("service1", schedulerConfig);
			serviceConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
			var profile = CreateProfile(1, null, schedulerConfig);

			// add 4 services to the profile
			profile.Services.Add(GetProfileConfiguration(serviceConfig, profile));
			profile.Services.Add(GetProfileConfiguration(serviceConfig, profile));
			profile.Services.Add(GetProfileConfiguration(serviceConfig, profile));
			profile.Services.Add(GetProfileConfiguration(serviceConfig, profile));

			PrintConfig(schedulerConfig);
			#endregion

			var scheduler = new Scheduler(env, schedulerConfig);
			scheduler.Schedule();

			// assert if was inserted into scheduled or unscheduled
			Assert.IsTrue(scheduler.ScheduledServices.Count == 4);
			Assert.IsTrue(scheduler.UnscheduledServices.Count == 0);
		}

		/// <summary>
		/// This test checks if services are rescheduled after previous services finished before expected time
		/// </summary>
		[TestMethod]
		public void TestRescheduleService(bool startHost = true)
		{
			Debug.WriteLine(DateTime.Now + ": Start Reschedule test");

			var env = CreateEnvironment(startHost: startHost);
			var schedulerConfig = GenerateBaseSchedulerConfig();
			schedulerConfig.DefaultExecutionTime = new TimeSpan(0, 5, 0);
			schedulerConfig.RescheduleInterval = new TimeSpan(0, 1, 0);

			#region Config
			var googleAdwordsAutoPlacementsConfig = CreateWorkflowServiceConfig("Google.AdWords.AutomaticPlacements", schedulerConfig);
			googleAdwordsAutoPlacementsConfig.SchedulingRules[0].Times[0] = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
			googleAdwordsAutoPlacementsConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 0, 0);

			// create 5 different profiles with the same service config
			for (var i = 1; i < 6; i++)
			{
				var profile = CreateProfile(i, null, schedulerConfig);
				profile.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profile));
			}
			#endregion

			var scheduler = new Scheduler(env, schedulerConfig);
			scheduler.Start();
		}

		#region Integration Test
		/// <summary>
		/// Full integration test with scenario accoridng to real configuration
		/// including Scheduler.Start(), Scheduler.Stop(), Scheduler.AddRequest()
		/// </summary>
		/// <param name="startHost">if to start host to run services or htere is another EXE for env and services</param>
		[TestMethod]
		public void TestFullIntegration(bool startHost = true)
		{
			Debug.WriteLine(DateTime.Now + ": Start full integration test");

			var env = CreateEnvironment(startHost: startHost);
			var schedulerConfig = GenerateBaseSchedulerConfig();

			GetIntegrationTestConfig(schedulerConfig);
			
			var scheduler = new Scheduler(env, schedulerConfig);
			scheduler.Start();
		}

		private void GetIntegrationTestConfig(SchedulerConfiguration schedulerConfig)
		{
			#region Generic Services
			//-------------------------
			// generic services
			//-------------------------
			var googleAdwordsConfig = CreateWorkflowServiceConfig("Google.Adwords", schedulerConfig);
			googleAdwordsConfig.SchedulingRules[0].Times[0] = new TimeSpan(4, 0, 0);
			googleAdwordsConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 0, 0);

			var googleAdwordsAutoPlacementsConfig = CreateWorkflowServiceConfig("Google.AdWords.AutomaticPlacements", schedulerConfig);
			googleAdwordsAutoPlacementsConfig.SchedulingRules[0].Times[0] = new TimeSpan(4, 0, 0);
			googleAdwordsAutoPlacementsConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 0, 0);

			var facebookConfig = CreateWorkflowServiceConfig("Facebook", schedulerConfig);
			facebookConfig.SchedulingRules[0].Times[0] = new TimeSpan(14, 20, 0);
			facebookConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 15, 0);
			facebookConfig.Limits.MaxConcurrentPerTemplate = 2;
			facebookConfig.Limits.MaxConcurrentPerProfile = 2;

			var msAdCenterConfig = CreateWorkflowServiceConfig("Microsoft.AdCenter", schedulerConfig);
			msAdCenterConfig.SchedulingRules[0].Times[0] = new TimeSpan(2, 5, 0);
			msAdCenterConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 0, 0);

			var backofficeConfig = CreateWorkflowServiceConfig("Backoffice", schedulerConfig);
			backofficeConfig.SchedulingRules[0].Times[0] = new TimeSpan(4, 15, 0);
			backofficeConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 0, 0);

			#endregion

			#region Accounts
			//-------------------------
			// Accounts
			//-------------------------

			#region Easy Forex
			//-------------------------
			// Easy Forex
			//-------------------------
			var profileEf = CreateProfile(7, null, schedulerConfig, "Easy Forex");
			
			// 5 google adwords
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileEf));
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileEf));
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileEf));
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileEf));
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileEf));

			// 4 google adwords automatic placements
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileEf));
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileEf));
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileEf));
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileEf));

			// 1 facebook
			profileEf.Services.Add(GetProfileConfiguration(facebookConfig, profileEf));

			// 1 MS ad center
			profileEf.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileEf));

			// 2 backoffice with 4:20 and rerun on 11:10
			profileEf.Services.Add(GetProfileConfiguration(backofficeConfig, profileEf));
			profileEf.Services[profileEf.Services.Count-1].SchedulingRules[0].Times[0] = new TimeSpan(4, 20, 0);

			profileEf.Services.Add(GetProfileConfiguration(backofficeConfig, profileEf));
			profileEf.Services[profileEf.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(11, 10, 0);

			#endregion

			#region Option Rally
			//-------------------------
			// Option Rally
			//-------------------------
			var profileOr = CreateProfile(10035, null, schedulerConfig, "Option Rally");
			
			// 4 google adwords: 05:10, 05:11, 11:30 and 11:31
			profileOr.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(5, 10, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(5, 11, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(11, 30, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(11, 31, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			// 2 google adwords automatic placements
			profileOr.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileOr));
			profileOr.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileOr));

			// 2 facebook: 5:15 and 11:30
			profileOr.Services.Add(GetProfileConfiguration(facebookConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(5, 15, 0);

			profileOr.Services.Add(GetProfileConfiguration(facebookConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(11, 30, 0);

			// backoffice: every day: regular, 11:00, 19:00, 19:30, 20:00, 20:30 and every Tuesday: 9:00, 9:30, 10:00
			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));

			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(11, 0, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 50, 0);

			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(19, 0, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(19, 30, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(20, 0, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(20, 30, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0] = new SchedulingRule {Scope = SchedulingScope.Week, Days = new[] {2}, Times = new[] {new TimeSpan(9, 0, 0)}};
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0] = new SchedulingRule { Scope = SchedulingScope.Week, Days = new[] {2}, Times = new[] {new TimeSpan(9, 30, 0)}};
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0] = new SchedulingRule { Scope = SchedulingScope.Week, Days = new[] {2}, Times = new[] {new TimeSpan(10, 0, 0)}};
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 46, 0);

			#endregion

			#region InterTrader
			//-------------------------
			// InterTrader
			//-------------------------
			var profileIt = CreateProfile(1239, null, schedulerConfig, "InterTrader");

			// 2 google adwords: regular and every Monday on 7:30
			profileIt.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileIt));

			profileIt.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileIt));
			profileIt.Services[profileIt.Services.Count - 1].SchedulingRules[0] = new SchedulingRule { Scope = SchedulingScope.Week, Days = new[] {1}, Times = new[] {new TimeSpan(7, 30, 0)}};
			profileIt.Services[profileIt.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 45, 0);

			// 1 google adwords automatic placements
			profileIt.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileIt));

			#endregion

			#region Stock Pair
			//-------------------------
			// Stock Pair
			//-------------------------
			var profileSp = CreateProfile(1249, null, schedulerConfig, "Stock Pair");

			// 1 google adwords
			profileSp.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileSp));

			// 1 google adwords automatic placements
			profileSp.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileSp));

			// 1 MS ad center
			profileSp.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileSp));

			// 1 backoffice
			profileSp.Services.Add(GetProfileConfiguration(backofficeConfig, profileSp));

			#endregion

			#region harmon.ie
			//-------------------------
			// harmon.ie
			//-------------------------
			var profileH = CreateProfile(1240, null, schedulerConfig, "harmon.ie");

			// 3 google adwords
			profileH.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileH));
			profileH.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileH));
			profileH.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileH));

			// 1 google adwords automatic placements
			profileH.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileH));

			// 1 MS ad center
			profileH.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileH));

			// 3 backoffice: regular, every day on 11:40 and every Sunday on 7:30
			profileH.Services.Add(GetProfileConfiguration(backofficeConfig, profileH));
			
			profileH.Services.Add(GetProfileConfiguration(backofficeConfig, profileH));
			profileH.Services[profileH.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(11, 40, 0);
			profileH.Services[profileH.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 10, 0);

			profileH.Services.Add(GetProfileConfiguration(backofficeConfig, profileH));
			profileH.Services[profileH.Services.Count - 1].SchedulingRules[0] = new SchedulingRule { Scope = SchedulingScope.Week, Days = new[] {0}, Times = new[] { new TimeSpan(7, 30, 0) } };
			profileH.Services[profileH.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 10, 0);

			#endregion

			#region Opteck
			//-------------------------
			// Opteck
			//-------------------------
			var profileOp = CreateProfile(1240235, null, schedulerConfig, "Opteck");
			
			// 1 google adwords
			profileOp.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileOp));

			// 1 facebook
			profileOp.Services.Add(GetProfileConfiguration(facebookConfig, profileOp));
			
			// 1 MS ad center
			profileOp.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileOp));

			// 2 backoffice: regular and every day on 11:20
			profileOp.Services.Add(GetProfileConfiguration(backofficeConfig, profileOp));

			profileOp.Services.Add(GetProfileConfiguration(backofficeConfig, profileOp));
			profileOp.Services[profileOp.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(11, 20, 0);
			profileOp.Services[profileOp.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 40, 0);

			#endregion

			#region Mansion
			//-------------------------
			// Mansion
			//-------------------------
			var profileMansion = CreateProfile(1240248, null, schedulerConfig, "Mansion");
			
			// 13 google adwords every 3 min between 4:05 - 4:45
			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(4, 5, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(4, 8, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(4, 11, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(4, 14, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(4, 17, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(4, 20, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(4, 23, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(4, 26, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(4, 29, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(4, 32, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(4, 35, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(4, 38, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(4, 41, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			// 4 MS ad center
			profileMansion.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileMansion));
			profileMansion.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileMansion));
			profileMansion.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileMansion));
			profileMansion.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileMansion));

			#endregion

			#region GreenSQL
			//-------------------------
			// GreenSQL
			//-------------------------
			var profileGreen = CreateProfile(1240250, null, schedulerConfig, "GreenSQL");

			// 1 google adwords
			profileGreen.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileGreen));

			// 1 google adwords automatic placements
			profileGreen.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileGreen));

			// 3 backoffice: every day on 4:20, every Sunday on 7:00, every 2nd of the month on 9:00
			profileGreen.Services.Add(GetProfileConfiguration(backofficeConfig, profileGreen));
			profileGreen.Services[profileGreen.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(4, 20, 0);

			profileGreen.Services.Add(GetProfileConfiguration(backofficeConfig, profileGreen));
			profileGreen.Services[profileGreen.Services.Count - 1].SchedulingRules[0] = new SchedulingRule { Scope = SchedulingScope.Week, Days = new[] {0}, Times = new[] {new TimeSpan(7, 0, 0)}};
			profileGreen.Services[profileGreen.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 15, 0);

			profileGreen.Services.Add(GetProfileConfiguration(backofficeConfig, profileGreen));
			profileGreen.Services[profileGreen.Services.Count - 1].SchedulingRules[0] = new SchedulingRule { Scope = SchedulingScope.Month, Days = new[] {2}, Times = new[] {new TimeSpan(9, 0, 0)}};

			#endregion

			#region Bbinary
			//-------------------------
			// Bbinary
			//-------------------------
			var profileBb = CreateProfile(1006, null, schedulerConfig, "Bbinary");

			// 2 google adwords: 9:10 and 16:50
			profileBb.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileBb));
			profileBb.Services[profileBb.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(9, 10, 0);

			profileBb.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileBb));
			profileBb.Services[profileBb.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(16, 50, 0);
			profileBb.Services[profileBb.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 45, 0);

			// 1 facebook
			profileBb.Services.Add(GetProfileConfiguration(facebookConfig, profileBb));

			// 1 MS ad center
			profileBb.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileBb));

			// 2 backoffice: regular and every day on 11:10 
			profileBb.Services.Add(GetProfileConfiguration(backofficeConfig, profileBb));

			profileBb.Services.Add(GetProfileConfiguration(backofficeConfig, profileBb));
			profileBb.Services[profileBb.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(11, 20, 0);
			
			#endregion

			#region Proportzia
			//-------------------------
			// Proportzia
			//-------------------------
			var profilePr = CreateProfile(42, null, schedulerConfig, "Proportzia");

			// 1 google adwords
			profilePr.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profilePr));
			
			// 1 google adwords automatic placements
			profilePr.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profilePr));

			// 1 facebook
			profilePr.Services.Add(GetProfileConfiguration(facebookConfig, profilePr));

			#endregion
			
			#endregion
			
			PrintConfig(schedulerConfig);
		}

		#endregion

		#region Stress Test
		/// <summary>
		/// Full configuration planned within one hour
		/// </summary>
		/// <param name="startHost">if to start host to run services or htere is another EXE for env and services</param>
		[TestMethod]
		public void StressTest(bool startHost = true)
		{
			Debug.WriteLine(DateTime.Now + ": Start stress test");

			var env = CreateEnvironment(startHost: startHost);
			var schedulerConfig = GenerateBaseSchedulerConfig();
			schedulerConfig.DefaultExecutionTime = new TimeSpan(0,5,0);

			GetStressTestConfig(schedulerConfig);

			var scheduler = new Scheduler(env, schedulerConfig);
			scheduler.Start();
		}

		private void GetStressTestConfig(SchedulerConfiguration schedulerConfig)
		{
			var stressHour = 9;// DateTime.Now.Hour + 1 < 24 ? DateTime.Now.Hour + 1 : 0;

			#region Generic Services
			//-------------------------
			// generic services
			//-------------------------
			var googleAdwordsConfig = CreateWorkflowServiceConfig("Google.Adwords", schedulerConfig);
			googleAdwordsConfig.SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 0, 0);
			googleAdwordsConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 0, 0);

			var googleAdwordsAutoPlacementsConfig = CreateWorkflowServiceConfig("Google.AdWords.AutomaticPlacements", schedulerConfig);
			googleAdwordsAutoPlacementsConfig.SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 0, 0);
			googleAdwordsAutoPlacementsConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 0, 0);

			var facebookConfig = CreateWorkflowServiceConfig("Facebook", schedulerConfig);
			facebookConfig.SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 20, 0);
			facebookConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 15, 0);
			facebookConfig.Limits.MaxConcurrentPerTemplate = 2;
			facebookConfig.Limits.MaxConcurrentPerProfile = 2;

			var msAdCenterConfig = CreateWorkflowServiceConfig("Microsoft.AdCenter", schedulerConfig);
			msAdCenterConfig.SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 5, 0);
			msAdCenterConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 0, 0);

			var backofficeConfig = CreateWorkflowServiceConfig("Backoffice", schedulerConfig);
			backofficeConfig.SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 15, 0);
			backofficeConfig.SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 0, 0);

			#endregion

			#region Accounts
			//-------------------------
			// Accounts
			//-------------------------

			#region Easy Forex
			//-------------------------
			// Easy Forex
			//-------------------------
			var profileEf = CreateProfile(7, null, schedulerConfig, "Easy Forex");

			// 5 google adwords
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileEf));
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileEf));
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileEf));
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileEf));
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileEf));

			// 4 google adwords automatic placements
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileEf));
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileEf));
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileEf));
			profileEf.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileEf));

			// 1 facebook
			profileEf.Services.Add(GetProfileConfiguration(facebookConfig, profileEf));

			// 1 MS ad center
			profileEf.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileEf));

			// 2 backoffice with 4:20 and rerun on 11:10
			profileEf.Services.Add(GetProfileConfiguration(backofficeConfig, profileEf));
			profileEf.Services[profileEf.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 20, 0);

			profileEf.Services.Add(GetProfileConfiguration(backofficeConfig, profileEf));
			profileEf.Services[profileEf.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 10, 0);

			#endregion

			#region Option Rally
			//-------------------------
			// Option Rally
			//-------------------------
			var profileOr = CreateProfile(10035, null, schedulerConfig, "Option Rally");

			// 4 google adwords: 05:10, 05:11, 11:30 and 11:31
			profileOr.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 10, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 11, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 30, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 31, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			// 2 google adwords automatic placements
			profileOr.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileOr));
			profileOr.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileOr));

			// 2 facebook: 5:15 and 11:30
			profileOr.Services.Add(GetProfileConfiguration(facebookConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 15, 0);

			profileOr.Services.Add(GetProfileConfiguration(facebookConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 30, 0);

			// backoffice: every day: regular, 11:00, 19:00, 19:30, 20:00, 20:30 and every Tuesday: 9:00, 9:30, 10:00
			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));

			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 0, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 50, 0);

			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 0, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 30, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 0, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 30, 0);
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(1, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0] = new SchedulingRule { Scope = SchedulingScope.Week, Days = new[] { 2 }, Times = new[] { new TimeSpan(stressHour, 0, 0) } };
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0] = new SchedulingRule { Scope = SchedulingScope.Week, Days = new[] { 2 }, Times = new[] { new TimeSpan(stressHour, 30, 0) } };
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileOr.Services.Add(GetProfileConfiguration(backofficeConfig, profileOr));
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0] = new SchedulingRule { Scope = SchedulingScope.Week, Days = new[] { 2 }, Times = new[] { new TimeSpan(stressHour, 0, 0) } };
			profileOr.Services[profileOr.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 46, 0);

			#endregion

			#region InterTrader
			//-------------------------
			// InterTrader
			//-------------------------
			var profileIt = CreateProfile(1239, null, schedulerConfig, "InterTrader");

			// 2 google adwords: regular and every Monday on 7:30
			profileIt.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileIt));

			profileIt.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileIt));
			profileIt.Services[profileIt.Services.Count - 1].SchedulingRules[0] = new SchedulingRule { Scope = SchedulingScope.Week, Days = new[] { 1 }, Times = new[] { new TimeSpan(stressHour, 30, 0) } };
			profileIt.Services[profileIt.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 45, 0);

			// 1 google adwords automatic placements
			profileIt.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileIt));

			#endregion

			#region Stock Pair
			//-------------------------
			// Stock Pair
			//-------------------------
			var profileSp = CreateProfile(1249, null, schedulerConfig, "Stock Pair");

			// 1 google adwords
			profileSp.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileSp));

			// 1 google adwords automatic placements
			profileSp.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileSp));

			// 1 MS ad center
			profileSp.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileSp));

			// 1 backoffice
			profileSp.Services.Add(GetProfileConfiguration(backofficeConfig, profileSp));

			#endregion

			#region harmon.ie
			//-------------------------
			// harmon.ie
			//-------------------------
			var profileH = CreateProfile(1240, null, schedulerConfig, "harmon.ie");

			// 3 google adwords
			profileH.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileH));
			profileH.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileH));
			profileH.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileH));

			// 1 google adwords automatic placements
			profileH.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileH));

			// 1 MS ad center
			profileH.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileH));

			// 3 backoffice: regular, every day on 11:40 and every Sunday on 7:30
			profileH.Services.Add(GetProfileConfiguration(backofficeConfig, profileH));

			profileH.Services.Add(GetProfileConfiguration(backofficeConfig, profileH));
			profileH.Services[profileH.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 40, 0);
			profileH.Services[profileH.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 10, 0);

			profileH.Services.Add(GetProfileConfiguration(backofficeConfig, profileH));
			profileH.Services[profileH.Services.Count - 1].SchedulingRules[0] = new SchedulingRule { Scope = SchedulingScope.Week, Days = new[] { 0 }, Times = new[] { new TimeSpan(stressHour, 30, 0) } };
			profileH.Services[profileH.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 10, 0);

			#endregion

			#region Opteck
			//-------------------------
			// Opteck
			//-------------------------
			var profileOp = CreateProfile(1240235, null, schedulerConfig, "Opteck");

			// 1 google adwords
			profileOp.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileOp));

			// 1 facebook
			profileOp.Services.Add(GetProfileConfiguration(facebookConfig, profileOp));

			// 1 MS ad center
			profileOp.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileOp));

			// 2 backoffice: regular and every day on 11:20
			profileOp.Services.Add(GetProfileConfiguration(backofficeConfig, profileOp));

			profileOp.Services.Add(GetProfileConfiguration(backofficeConfig, profileOp));
			profileOp.Services[profileOp.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 20, 0);
			profileOp.Services[profileOp.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 40, 0);

			#endregion

			#region Mansion
			//-------------------------
			// Mansion
			//-------------------------
			var profileMansion = CreateProfile(1240248, null, schedulerConfig, "Mansion");

			// 13 google adwords every 3 min between 4:05 - 4:45
			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 5, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 8, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 11, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 14, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 17, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 20, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 23, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 26, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 29, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 32, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 35, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 38, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			profileMansion.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileMansion));
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 41, 0);
			profileMansion.Services[profileMansion.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 30, 0);

			// 4 MS ad center
			profileMansion.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileMansion));
			profileMansion.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileMansion));
			profileMansion.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileMansion));
			profileMansion.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileMansion));

			#endregion

			#region GreenSQL
			//-------------------------
			// GreenSQL
			//-------------------------
			var profileGreen = CreateProfile(1240250, null, schedulerConfig, "GreenSQL");

			// 1 google adwords
			profileGreen.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileGreen));

			// 1 google adwords automatic placements
			profileGreen.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profileGreen));

			// 3 backoffice: every day on 4:20, every Sunday on 7:00, every 2nd of the month on 9:00
			profileGreen.Services.Add(GetProfileConfiguration(backofficeConfig, profileGreen));
			profileGreen.Services[profileGreen.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 20, 0);

			profileGreen.Services.Add(GetProfileConfiguration(backofficeConfig, profileGreen));
			profileGreen.Services[profileGreen.Services.Count - 1].SchedulingRules[0] = new SchedulingRule { Scope = SchedulingScope.Week, Days = new[] { 0 }, Times = new[] { new TimeSpan(stressHour, 0, 0) } };
			profileGreen.Services[profileGreen.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 15, 0);

			profileGreen.Services.Add(GetProfileConfiguration(backofficeConfig, profileGreen));
			profileGreen.Services[profileGreen.Services.Count - 1].SchedulingRules[0] = new SchedulingRule { Scope = SchedulingScope.Month, Days = new[] { 2 }, Times = new[] { new TimeSpan(stressHour, 0, 0) } };

			#endregion

			#region Bbinary
			//-------------------------
			// Bbinary
			//-------------------------
			var profileBb = CreateProfile(1006, null, schedulerConfig, "Bbinary");

			// 2 google adwords: 9:10 and 16:50
			profileBb.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileBb));
			profileBb.Services[profileBb.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 10, 0);

			profileBb.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profileBb));
			profileBb.Services[profileBb.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 50, 0);
			profileBb.Services[profileBb.Services.Count - 1].SchedulingRules[0].MaxDeviationAfter = new TimeSpan(0, 45, 0);

			// 1 facebook
			profileBb.Services.Add(GetProfileConfiguration(facebookConfig, profileBb));

			// 1 MS ad center
			profileBb.Services.Add(GetProfileConfiguration(msAdCenterConfig, profileBb));

			// 2 backoffice: regular and every day on 11:10 
			profileBb.Services.Add(GetProfileConfiguration(backofficeConfig, profileBb));

			profileBb.Services.Add(GetProfileConfiguration(backofficeConfig, profileBb));
			profileBb.Services[profileBb.Services.Count - 1].SchedulingRules[0].Times[0] = new TimeSpan(stressHour, 20, 0);

			#endregion

			#region Proportzia
			//-------------------------
			// Proportzia
			//-------------------------
			var profilePr = CreateProfile(42, null, schedulerConfig, "Proportzia");

			// 1 google adwords
			profilePr.Services.Add(GetProfileConfiguration(googleAdwordsConfig, profilePr));

			// 1 google adwords automatic placements
			profilePr.Services.Add(GetProfileConfiguration(googleAdwordsAutoPlacementsConfig, profilePr));

			// 1 facebook
			profilePr.Services.Add(GetProfileConfiguration(facebookConfig, profilePr));

			#endregion

			#endregion

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
					RescheduleInterval = new TimeSpan(0, 2, 0),
					ExecuteInterval = new TimeSpan(0, 0, 1),
					CheckUnplannedServicesInterval = new TimeSpan(0, 0, 5),
					ExecutionStatisticsRefreshInterval = new TimeSpan(0, 10, 0),
					DefaultExecutionTime = new TimeSpan(0, 3, 0),
					ServiceConfigurationList = new List<ServiceConfiguration>(),
					Profiles = new ProfilesCollection()
				};

			return schedulerConfig;
		}

		private ServiceEnvironment CreateEnvironment(bool cleanRecovery = true, bool startHost = true)
		{
			// create service env
			var envConfig = new ServiceEnvironmentConfiguration
			{
				DefaultHostName = "Johnny",
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

			var environment = ServiceEnvironment.Open("Scheduler Test", envConfig);
			//CleanEnvEvents(environment);

			if (startHost)
			{
				var host = new ServiceExecutionHost(environment.EnvironmentConfiguration.DefaultHostName, environment);
			}

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

			// set default empty scheduling for workflow service
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

		private ServiceConfiguration CreateWorkflowServiceConfig(string serviceName, SchedulerConfiguration schedulerConfig)
		{
			// workflow step definition
			var stepConfig = new ServiceConfiguration
			{
				ServiceClass = typeof(TestService).AssemblyQualifiedName
			};

			// workflow definition
			var workflowConfig = new WorkflowServiceConfiguration
			{
				ServiceName = serviceName,
				Workflow = new WorkflowNodeGroup
				{
					Mode = WorkflowNodeGroupMode.Linear,
					Nodes = new LockableList<WorkflowNode>
								{
									new WorkflowStep {Name = "Initializer", ServiceConfiguration = stepConfig},
									new WorkflowStep {Name = "Retriever", ServiceConfiguration = stepConfig},
									new WorkflowStep {Name = "Processor", ServiceConfiguration = stepConfig},
									new WorkflowStep {Name = "Staging", ServiceConfiguration = stepConfig},
								}
				}
			};

			// set default empty scheduling for workflow service
			workflowConfig.SchedulingRules.Add(new SchedulingRule
			{
				Scope = SchedulingScope.Day,
				Times = new[] { new TimeSpan(0, 0, 0) },
				Days = new[] { 0 }
			});
			
			// add to service list in configuration
			schedulerConfig.ServiceConfigurationList.Add(workflowConfig);

			return workflowConfig;
		}

		private ServiceProfile CreateProfile(int accountId, ServiceConfiguration serviceConfig, SchedulerConfiguration schedulerConfig, string profileName = "profile")
		{
			var profile = new ServiceProfile { Name =  String.Format("{0}-{1}", profileName, accountId)};
			profile.Parameters["AccountID"] = accountId;

			if (serviceConfig != null)
			{
				profile.Services.Add(GetProfileConfiguration(serviceConfig, profile));
			}
			schedulerConfig.Profiles.Add(profile);

			return profile;
		}

		private ServiceConfiguration GetProfileConfiguration(ServiceConfiguration serviceConfig, ServiceProfile profile)
		{
			var config = profile.DeriveConfiguration(serviceConfig);
			config.ConfigurationID = GetGuidFromString(String.Format("{0}-{1}-{2}",profile.Name, serviceConfig.ServiceName, profile.Services.Count + 1));
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

		private void CleanEnvEvents(ServiceEnvironment environment)
		{
			var env = environment.EnvironmentConfiguration;
			using (var connection = new SqlConnection(env.ConnectionString))
			{
				var command = new SqlCommand("delete from dbo.ServiceEnvironmentEvent", connection)
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
