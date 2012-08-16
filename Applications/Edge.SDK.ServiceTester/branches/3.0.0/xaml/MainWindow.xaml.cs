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

namespace Edge.SDK.ServiceTester
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			this.DataContext = App.BindingData;
		}

		private void _Toolbar_Start_Click(object sender, RoutedEventArgs e)
		{
			/*
			var service = _Tree.SelectedItem as ServiceDisplayInfo;
			if (service == null)
				return;

			service.Start();
			*/
		}
	}
}
