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
using Edge.Core.Configuration;
using Edge.Core.Services;
using Edge.Data.Pipeline;

namespace Edge.SDK.ServiceTester
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static BindingData BindingData = new BindingData();
		public static DeliveryDBServer DeliveryServer = new DeliveryDBServer();

		protected override void OnStartup(StartupEventArgs e)
		{
			// Get an alternate file name
			string configFileName = EdgeServicesConfiguration.DefaultFileName;
			if (e.Args.Length > 0 && e.Args[0].StartsWith("/") && e.Args[0].Length > 1)
			{
				configFileName = e.Args[0].Substring(1);
			}

			#if !DEBUG // change to !DEBUG
			try
			{
				EdgeServicesConfiguration.Load(configFileName);
			}
			catch (Exception ex)
			{
				
				MessageBox.Show
				(
					messageBoxText:	String.Format("Error loading the configuration file {0}\n\n({1}: {2})",
						Path.Combine(Directory.GetCurrentDirectory(), configFileName),
						ex.GetType().Name,
						ex.Message),
					caption: "Error",
					button: MessageBoxButton.OK,
					icon: MessageBoxImage.Error
				);
				Shutdown();
				return;
			}
			#else
			EdgeServicesConfiguration.Load(configFileName);
			#endif

			BindingData.Services = new ObservableCollection<ServiceDisplayInfo>();
			foreach (ServiceElement serviceConfig in EdgeServicesConfiguration.Current.Services)
			{
				if (serviceConfig.IsPublic)
					BindingData.Services.Add(new ServiceDisplayInfo(serviceConfig));
			}

			DeliveryServer.Start();

			// Auto start if there is only a single service
			if (BindingData.Services.Count == 1)
				BindingData.Services[0].Start();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);
			DeliveryServer.Stop();
		}
	}

	public class BindingData
	{
		public ObservableCollection<ServiceDisplayInfo> Services { get; set; }
	}

	
}
