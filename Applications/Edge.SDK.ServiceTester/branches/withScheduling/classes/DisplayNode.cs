using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Edge.Core.Services;
using System.ComponentModel;
using Edge.Core.Configuration;
using System.Collections.ObjectModel;

namespace Edge.SDK.ServiceTester
{
	public class DisplayNode:INotifyPropertyChanged
	{
		#region props
		// ...................

		public event PropertyChangedEventHandler PropertyChanged;

		public int? AccountID
		{
			get;
			private set;
		}

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

		public DisplayNode Parent
		{
			get;
			private set;
		}

		public DisplayNode Root
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


		WorkflowStepElement _step = null;

		public bool IsEnabled
		{
			get { return _step == null ? true : _step.IsEnabled; }
			set { _step.IsEnabled = value; }
		}

		string _customStatus = null;

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

		public ObservableCollection<DisplayNode> Children
		{
			get;
			private set;
		}

		// ...................
		#endregion

		#region ctors
		// ...................

		private DisplayNode()
		{
			Children = new ObservableCollection<DisplayNode>();
		}

		public DisplayNode(AccountElement account):this()
		{
			AccountID = account.ID;
			Name = string.Format("{0} ({1})", account.Name, account.ID);
			foreach (AccountServiceElement service in account.Services)
				Children.Add(new DisplayNode(new ActiveServiceElement(service), accountID: this.AccountID));
			IsExpanded = false;
		}

		public DisplayNode(ServiceElement configuration, string name = null, int? accountID = null):this()
		{
			Configuration = configuration;
			Name = name ?? Configuration.Name;
			AccountID = accountID;
			if (Configuration.Workflow != null)
			{
				foreach (WorkflowStepElement step in Configuration.Workflow)
				{
					if (step.BaseConfiguration.Element == null)
						Children.Add(new DisplayNode(step.ActualName, "(undefined base)") {  Parent = this });
					else
						Children.Add(new DisplayNode(step.BaseConfiguration.Element, step) { Parent = this });
				}
			}
			IsExpanded = true;
		}

		public DisplayNode(ServiceElement configuration, WorkflowStepElement step):this(configuration, step.Name)
		{
			_step = step;
		}

		public DisplayNode(string name, string status):this()
		{
			Name = name;
			_customStatus = status;
			IsExpanded = true;
		}

		public void Start(Dictionary<string,string> options = null)
		{
			ServiceElement configuration = options == null ?
				this.Configuration :
				new ActiveServiceElement(this.Configuration);

			if (options != null)
				configuration.Options.Merge(options);

			if (this.AccountID == null)
				Start(Service.CreateInstance(configuration));
			else
				Start(Service.CreateInstance(configuration, this.AccountID.Value));
		}

		// ...................
		#endregion

		#region internal
		// ...................

		void Start(ServiceInstance instance)
		{
			if (Instance != null)
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
			DisplayNode step = this.Children.Single(item => item.Name == e.ServiceName);
			if (step != null)
			{
				step.Start(e.RequestedService);
			}
			else
			{
				step = new DisplayNode(e.RequestedService.Configuration) { Unplanned = true };
				step.Start(e.RequestedService);
				this.Children.Add(step);
			}
		}

		// ...................
		#endregion 
	}
}
