using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace Edge.SDK.ServiceTester
{
	public static class AppData
	{
		public static readonly string AppDataDirectory;
		static AppData()
		{
			AppDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Edge\\Suite\\ServiceTester");
			if (!Directory.Exists(AppDataDirectory))
				Directory.CreateDirectory(AppDataDirectory);
		}

		public static T Load<T>(string entryName) where T: class
		{
			string file = Path.Combine(AppDataDirectory, Uri.EscapeUriString(entryName) + ".xml");
			if (!File.Exists(file))
				return null;

			XmlSerializer serializer = new XmlSerializer(typeof(T));
			using (FileStream stream = File.Open(file, FileMode.Open))
			{
				return (T)serializer.Deserialize(stream);
			}
		}

		public static void Save(string entryName, object value)
		{
			string file = Path.Combine(AppDataDirectory, Uri.EscapeUriString(entryName) + ".xml");
			XmlSerializer serializer = new XmlSerializer(value.GetType());
			using (FileStream stream = File.Open(file, FileMode.Create))
			{
				serializer.Serialize(stream, value);
			}
		}
	}
}
