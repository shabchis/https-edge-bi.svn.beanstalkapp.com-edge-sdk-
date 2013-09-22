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
using System.Windows.Controls;
using System.Text;

namespace Edge.SDK.ServiceTester
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static BindingData BindingData = new BindingData() { SelectedAccount = new AccountDisplayInfo() };

		protected override void OnStartup(StartupEventArgs e)
		{
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

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
			EdgeServicesConfiguration.Load(configFileName, readOnly: false);
			#endif

			BindingData.Services = new ObservableCollection<ServiceDisplayInfo>();
			foreach (ServiceElement serviceConfig in EdgeServicesConfiguration.Current.Services)
			{
				if (serviceConfig.IsPublic)
					BindingData.Services.Add(new ServiceDisplayInfo(serviceConfig));
			}

			BindingData.Accounts = new ObservableCollection<AccountDisplayInfo>();
			BindingData.Accounts.Add(BindingData.SelectedAccount);
			foreach (AccountElement accountConfig in EdgeServicesConfiguration.Current.Accounts)
			{
				BindingData.Accounts.Add(new AccountDisplayInfo() { AccountConfig = accountConfig });
			}

			// Remember last selected account
			string lastUsedAccount = AppData.Load<string>("BindingData.SelectedAccount");
			if (lastUsedAccount != null)
			{
				int accountID;
				AccountElement accountConfig;
				if (Int32.TryParse(lastUsedAccount, out accountID) && (accountConfig = EdgeServicesConfiguration.Current.Accounts.GetAccount(accountID)) != null)
					BindingData.SelectedAccount = BindingData.Accounts.First(acc => acc.AccountConfig == accountConfig);
			}

			BindingData.PropertyChanged += new PropertyChangedEventHandler((sender, args) =>
			{
				if (args.PropertyName == "SelectedAccount")
					AppData.Save("BindingData.SelectedAccount", BindingData.SelectedAccount.AccountConfig.ID.ToString());
			});
		}

		Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			string path = Path.Combine(Environment.CurrentDirectory, args.Name + ".dll");
			try { return Assembly.LoadFrom(path); }
			catch { return null; }
		}
	}

	public class AccountDisplayInfo
	{
		public AccountElement AccountConfig { get; set; }

		public override string ToString()
		{
			return this.AccountConfig == null ? "Accounts disabled" : String.Format("{0} - {1}", this.AccountConfig.ID, this.AccountConfig.Name);
		}
	}

	public class BindingData: INotifyPropertyChanged
	{
		AccountDisplayInfo _account;
		public AccountDisplayInfo SelectedAccount
		{
			get { return _account; }
			set
			{
				_account = value;
				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("SelectedAccount"));
			}
		}

		public ObservableCollection<AccountDisplayInfo> Accounts { get; set; }
		public ObservableCollection<ServiceDisplayInfo> Services { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;
	}

	
}
