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

namespace Edge.SDK.ServiceTester
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public ConsoleWriter LogWriter
		{
			get;
			private set;
		}

		public MainWindow()
		{
			InitializeComponent();
			this.DataContext = App.BindingData;

			App.DeliveryServer.Start(new ConsoleWriter() { Textbox = _Console});

			// Auto start if there is only a single service
			if (App.BindingData.Services.Count == 1)
				App.BindingData.Services[0].Start();
		}

		private void _Toolbar_Start_Click(object sender, RoutedEventArgs e)
		{
			var service = _Tree.SelectedItem as ServiceDisplayInfo;
			if (service == null)
				return;

			service.Start();
		}
	}

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
	


}
