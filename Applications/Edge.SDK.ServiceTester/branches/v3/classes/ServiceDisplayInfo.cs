using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Edge.Core.Services2;

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

		public ServiceConfiguration Configuration
		{
			get;
			private set;
		}

		public Guid InstanceID
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

		public double Progress
		{
			get { return Instance != null ? Instance.Progress*100 : 0; }
		}

		public string Output
		{
			get;
			private set;
		}

		string _customStatus;

		public string Status
		{
			get
			{
				if (_customStatus != null)
					return _customStatus;
				else if (Instance == null)
					return string.Empty;
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

		public ServiceDisplayInfo(ServiceConfiguration configuration, string name = null)
		{
			Configuration = configuration;
			Name = name ?? Configuration.ServiceName;

			/*
			Children = new ObservableCollection<ServiceDisplayInfo>();
			if (Configuration.Workflow != null)
			{
				foreach (WorkflowStepElement step in Configuration.Workflow)
				{
					if (step.BaseConfiguration.Element == null)
						Children.Add(new ServiceDisplayInfo(step.ActualName, "(undefined base)"));
					else
						Children.Add(new ServiceDisplayInfo(step.BaseConfiguration.Element, step.ActualName));
				}
			}
			*/
		}

		public ServiceDisplayInfo(string name, string status)
		{
			Name = name;
			_customStatus = status;
		}

		public void Start()
		{
			Start(App.Environment.CreateServiceInstance(Configuration));
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
			Instance.StateChanged += new EventHandler(Instance_StateChanged);
			Instance.OutcomeReported += new EventHandler(Instance_OutcomeReported);
			Instance.ProgressReported += new EventHandler(Instance_ProgressReported);
			Instance.OutputGenerated += new EventHandler(Instance_OutputGenerated);
			//Instance.ChildServiceRequested += new EventHandler(Instance_ChildServiceRequested);

			NotifyPropertyChanged("Status");
			NotifyPropertyChanged("Progress");
		}


		void Instance_StateChanged(object sender, EventArgs e)
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

		void Instance_OutputGenerated(object sender, EventArgs e)
		{
			if (Instance.Output == null)
				return;

			if (Instance.Output is LogMessage)
			{
				var lm = (LogMessage)Instance.Output;
				if (lm.IsException)
					this.Output = String.Format("{0} ({1}). {2}", lm.Message, lm.Exception.GetType().Name, lm.Exception.Message);
				else
					this.Output = String.Format("{0} - {1}", lm.MessageType, lm.Message);
			}
			else
				this.Output = Instance.Output.ToString();

			NotifyPropertyChanged("Output");
		}

		/*
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
		*/

		// ...................
		#endregion
		
	}
	
}
