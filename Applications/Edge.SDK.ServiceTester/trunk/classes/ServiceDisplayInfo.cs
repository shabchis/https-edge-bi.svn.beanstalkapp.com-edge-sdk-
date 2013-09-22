using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Edge.Core.Services;
using System.ComponentModel;
using Edge.Core.Configuration;
using System.Collections.ObjectModel;
using Edge.Core;

namespace Edge.SDK.ServiceTester
{
	public class ServiceDisplayInfo:INotifyPropertyChanged
	{
		#region props
		// ...................

		public event PropertyChangedEventHandler PropertyChanged;

		public ServiceInstance Instance
		{
			get;
			private set;
		}

		public ServiceElement Configuration
		{
			get;
			private set;
		}

		public Dictionary<int, AccountServiceElement> Accounts
		{
			get;
			private set;
		}

		public long? InstanceID
		{
			get;
			private set;
		}

		public string Name
		{
			get;
			private set;
		}

		public bool Unplanned
		{
			get;
			private set;
		}

		public ServiceDisplayInfo Parent
		{
			get;
			private set;
		}

		public ServiceDisplayInfo Root
		{
			get
			{
				if (this.Parent == null)
					return this;
				else
					return this.Parent.Root;
			}
		}

		public double Progress
		{
			get { return Instance != null ? Instance.Progress*100 : 0; }
		
		}

		public bool IsDuplicate
		{
			get;
			internal set;
		}

		public bool IsExpanded
		{
			get;
			set;
		}

		public SettingsCollection Options
		{
			get;
			set;
		}


		WorkflowStepElement _step = null;

		public bool IsEnabled
		{
			get { return _step == null ? true : _step.IsEnabled; }
			set { _step.IsEnabled = value; }
		}

		string _customStatus;

		public string Status
		{
			get
			{
				if (_customStatus != null)
					return _customStatus;
				else if (Instance == null)
					return null;
				else
					return Instance.State == ServiceState.Ended ?
						Instance.Outcome.ToString() :
						Instance.State.ToString();
			}
		}

		public ObservableCollection<ServiceDisplayInfo> Children
		{
			get;
			private set;
		}

		// ...................
		#endregion

		#region ctors
		// ...................

		public ServiceDisplayInfo(ServiceElement configuration, string name = null)
		{
			Configuration = configuration;
			Name = name ?? Configuration.Name;

			Children = new ObservableCollection<ServiceDisplayInfo>();
			if (Configuration.Workflow != null)
			{
				foreach (WorkflowStepElement step in Configuration.Workflow)
				{
					if (step.BaseConfiguration.Element == null)
						Children.Add(new ServiceDisplayInfo(step.ActualName, "(undefined base)") {  Parent = this });
					else
						Children.Add(new ServiceDisplayInfo(step.BaseConfiguration.Element, step) { Parent = this });
				}
			}
			
			this.Accounts = new Dictionary<int,AccountServiceElement>();

			// Find per-account configurations
			foreach (AccountElement account in EdgeServicesConfiguration.Current.Accounts)
			{
				foreach (AccountServiceElement accountService in account.Services)
					if (accountService.Uses.Element == configuration)
						this.Accounts.Add(account.ID, accountService);
			}

			IsExpanded = true;
		}

		public ServiceDisplayInfo(ServiceElement configuration, WorkflowStepElement step):this(configuration, step.Name)
		{
			_step = step;
		}

		public ServiceDisplayInfo(string name, string status)
		{
			Name = name;
			_customStatus = status;
			IsExpanded = true;
		}

		public void Start(int? accountID = null, Dictionary<string,string> options = null)
		{
			ActiveServiceElement configuration;
			if (accountID == null)
			{
				configuration =	new ActiveServiceElement(this.Configuration);
				if (options != null)
					configuration.Options.Merge(options);

				Start(Service.CreateInstance(configuration));
			}
			else
			{
				configuration = new ActiveServiceElement(this.Accounts[accountID.Value]);
				if (options != null)
					configuration.Options.Merge(options);

				Start(Service.CreateInstance(configuration, accountID.Value));
			}
		}

		// ...................
		#endregion

		#region internal
		// ...................

		void Start(ServiceInstance instance)
		{
			if (Instance != null && Instance.State != ServiceState.Ended)
				throw new InvalidOperationException("Service is already started.");

			AttachToInstance(instance);
			Instance.Initialize();
		}

		void NotifyPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		void AttachToInstance(ServiceInstance instance)
		{
			Instance = instance;
			Instance.StateChanged += new EventHandler<ServiceStateChangedEventArgs>(Instance_StateChanged);
			Instance.OutcomeReported += new EventHandler(Instance_OutcomeReported);
			Instance.ProgressReported += new EventHandler(Instance_ProgressReported);
			Instance.ChildServiceRequested += new EventHandler<ServiceRequestedEventArgs>(Instance_ChildServiceRequested);

			NotifyPropertyChanged("Status");
			NotifyPropertyChanged("Progress");
		}

		void Instance_StateChanged(object sender, ServiceStateChangedEventArgs e)
		{
			NotifyPropertyChanged("Status");

			if (Instance.State == ServiceState.Ready)
			{
				InstanceID = Instance.InstanceID;
				NotifyPropertyChanged("InstanceID");
				Instance.Start();
			}
		}

		void Instance_OutcomeReported(object sender, EventArgs e)
		{
			NotifyPropertyChanged("Status");
		}

		void Instance_ProgressReported(object sender, EventArgs e)
		{
			NotifyPropertyChanged("Progress");
		}

		void Instance_ChildServiceRequested(object sender, ServiceRequestedEventArgs e)
		{
			ServiceDisplayInfo step = this.Children.Single(item => item.Name == e.ServiceName);
			if (step != null)
			{
				step.Start(e.RequestedService);
			}
			else
			{
				step = new ServiceDisplayInfo(e.RequestedService.Configuration) { Unplanned = true };
				step.Start(e.RequestedService);
				this.Children.Add(step);
			}
		}

		// ...................
		#endregion 
	}
}
