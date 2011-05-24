using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Edge.Core.Services2;
using System.Threading;
using WF = Edge.Core.Services2.Workflow;

namespace Edge.SDK.ServiceTester
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static BindingData BindingData = new BindingData();
		public static ServiceEnvironment Environment = new ServiceEnvironment();

		protected override void OnStartup(StartupEventArgs e)
		{
			ServiceConfiguration config = new ServiceConfiguration()
			{
				ServiceName = "Testing",
				ServiceType = "Edge.SDK.ServiceTester.TestService, Edge.SDK.ServiceTester",
			};

			BindingData.Services = new ObservableCollection<ServiceDisplayInfo>();
			BindingData.Services.Add(new ServiceDisplayInfo(config));

			// Auto start if there is only a single service
			if (BindingData.Services.Count == 1)
				BindingData.Services[0].Start();
		}

	}

	public class BindingData
	{
		public ObservableCollection<ServiceDisplayInfo> Services { get; set; }
	}

	public class TestService : Service
	{
		protected override ServiceOutcome DoWork()
		{
			for (int i = 1; i < 100; i++)
			{
				Thread.Sleep(TimeSpan.FromMilliseconds(50));
				Progress = ((double) i) / 100;
			}

			throw new InvalidOperationException("Can't do this shit here.");

			return ServiceOutcome.Success;
		}
	}

}
