using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Edge.Core.Configuration;
using Edge.Data.Pipeline.Configuration;
using System.Collections.ObjectModel;

namespace Edge.SDK.ServiceTester
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public static BindingData BindingData = new BindingData();

		public static MainWindow Current
		{
			get;
			private set;
		}

		public ConsoleWriter LogWriter
		{
			get;
			private set;
		}

		public MainWindow()
		{
			InitializeComponent();
			MainWindow.Current = this;

			MainWindow.BindingData.Services = new ObservableCollection<DisplayNode>();

			// Add master collection

			// Add accounts
			var accounts = new DisplayNode("Accounts", status: null);
			foreach (AccountElement accountConfig in EdgeServicesConfiguration.Current.Accounts)
			{
				accounts.Children.Add(new DisplayNode(accountConfig));
			}
			BindingData.Services.Add(accounts);

			this.DataContext = MainWindow.BindingData;

			App.DeliveryServer = new Data.Pipeline.DeliveryDBServer();
			App.DeliveryServer.Start(new ConsoleWriter() { Textbox = _Console});

			// Auto start if there is only a single service
			//if (App.BindingData.Services.Count == 1)
				//App.BindingData.Services[0].Start();
		}

		private void _Toolbar_Start_Click(object sender, RoutedEventArgs e)
		{
			var service = _Tree.SelectedItem as DisplayNode;
			if (service == null)
				return;

			service.Start();
		}

		private void _Toolbar_Advanced_Click(object sender, RoutedEventArgs e)
		{
			var service = _Tree.SelectedItem as DisplayNode;
			if (service == null)
				return;

			// always use the root service
			service = service.Root;

			var dialog = new BatchDialog();
			if (!dialog.Init(service))
				return;

			var result = dialog.ShowDialog();
			if (result != null && result.Value)
			{
				int startIndex = MainWindow.BindingData.Services.IndexOf(service);
				int count = 0;
				foreach(Dictionary<string,string> options in dialog.GetInstanceOptions())
				{
					var dup = new DisplayNode(service.Configuration) { IsDuplicate = true, IsExpanded = service.IsExpanded };
					MainWindow.BindingData.Services.Insert(startIndex + (++count), dup);
					dup.Start(options);
				}
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			Application.Current.Shutdown();
		}
	}

	public class BindingData
	{
		public ObservableCollection<DisplayNode> Services { get; set; }
	}

	#region Helper classes

	public class ConsoleWriter : TextWriter
	{
		Action<char> _write;

		public ConsoleWriter()
		{
			_write = value =>
			{
				this.Textbox.AppendText(value.ToString());
				this.Textbox.ScrollToEnd();
			};
		}

		public TextBox Textbox
		{
			get;
			set;
		}

		public override void Write(char value)
		{
			this.Textbox.Dispatcher.BeginInvoke(_write, value);
		}


		public override Encoding Encoding
		{
			get { return Encoding.UTF8; }
		}
	}

	public class NameTemplateSelector : DataTemplateSelector
	{
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			var service = (DisplayNode)item;
			if (service.Parent == null)
				return (DataTemplate) MainWindow.Current._Tree.Resources["NameTemplateRoot"];
			else
				return (DataTemplate) MainWindow.Current._Tree.Resources["NameTemplateChild"];
		}
	}
	#endregion

}
