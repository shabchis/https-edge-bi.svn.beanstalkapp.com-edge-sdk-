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
using System.Windows.Shapes;
using Edge.Data.Pipeline.Configuration;
using System.IO;
using System.Collections.ObjectModel;
using System.Configuration;

namespace Edge.SDK.ServiceTester
{
	/// <summary>
	/// Interaction logic for BatchDialog.xaml
	/// </summary>
	public partial class BatchDialog : Window
	{
		private List<string> _optionsHeaders;
		private ObservableCollection<List<BatchInstanceOption>> _instances;

		public DisplayNode CurrentService { get; private set; }

		public BatchDialog()
		{
			InitializeComponent();
		}

		public bool Init(DisplayNode service)
		{
			if (this.CurrentService == service)
				return true;

			this.DataContext = this.CurrentService = service;

			_optionsHeaders = new List<string>();
			ConfigurationElement o;
			if (!service.Configuration.Extensions.TryGetValue(OptionDefinitionCollection.ExtensionName, out o))
			{
				MessageBox.Show("No <OptionDefinitions> section defined in the service.", "Configuration error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			
			var options = o as OptionDefinitionCollection;
			foreach (OptionDefinition option in options)
				if (option.IsPublic)
					_optionsHeaders.Add(option.Name);
			

			var grid = this._ListView.View as GridView;

			for (int i = 0; i < _optionsHeaders.Count; i++)
			{
				string option = _optionsHeaders[i];
				DataTemplate cellTemplate = new DataTemplate();
				cellTemplate.VisualTree = new FrameworkElementFactory(typeof(Grid));
				var textbox = new FrameworkElementFactory(typeof(TextBox));
				textbox.SetBinding(TextBox.TextProperty, new Binding(String.Format("[{0}].Value", i)));
				cellTemplate.VisualTree.AppendChild(textbox);

				grid.Columns.Add(new GridViewColumn()
				{
					Header = option,
					CellTemplate = cellTemplate,
					Width=120
				});
			};

			this._instances = AppData.Load<ObservableCollection<List<BatchInstanceOption>>>(service.Name);
			if (this._instances == null)
			{
				this._instances = new ObservableCollection<List<BatchInstanceOption>>();
				this._instances.Add(NewInstance());
			}

			_ListView.ItemsSource = this._instances;

			return true;
		}


		private void _Toolbar_Add(object sender, RoutedEventArgs e)
		{
			var instances = (ObservableCollection<List<BatchInstanceOption>>)_ListView.ItemsSource;
			instances.Add(NewInstance());
		}

		private void _Item_Delete(object sender, RoutedEventArgs e)
		{
			var instances = (ObservableCollection<List<BatchInstanceOption>>)_ListView.ItemsSource;

			DependencyObject dep = (DependencyObject)e.OriginalSource;

			while ((dep != null) && !(dep is ListViewItem))
				dep = VisualTreeHelper.GetParent(dep);

			if (dep == null)
				return;

			var item = (List<BatchInstanceOption>)_ListView.ItemContainerGenerator.ItemFromContainer(dep);

			instances.Remove(item);
		}

		private List<BatchInstanceOption> NewInstance()
		{
			var exampleInstance = new List<BatchInstanceOption>();
			foreach (string option in _optionsHeaders)
				exampleInstance.Add(new BatchInstanceOption(option, string.Empty));
			return exampleInstance;
		}

		private void _Run_Click(object sender, RoutedEventArgs e)
		{
			var instances = (ObservableCollection<List<BatchInstanceOption>>)_ListView.ItemsSource;
			AppData.Save(((DisplayNode)this.DataContext).Name, instances);

			this.DialogResult = true;
		}

		public IEnumerable<Dictionary<string, string>> GetInstanceOptions()
		{
			foreach (List<BatchInstanceOption> optionBag in this._instances)
			{
				var vals = new Dictionary<string, string>();
				foreach (BatchInstanceOption option in optionBag)
					vals.Add(option.Option, option.Value);
				yield return vals;
			}
		}

	}

	[Serializable]
	public class BatchInstanceOption
	{
		public string Option { get; set; }
		public string Value { get; set; }

		public BatchInstanceOption()
		{
		}

		public BatchInstanceOption(string option, string value)
		{
			this.Option = option;
			this.Value = value;
		}
	}
	
}
