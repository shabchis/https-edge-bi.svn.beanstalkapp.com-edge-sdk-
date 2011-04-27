using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Flavor;

namespace Edge.SDK.VisualStudio
{
	[Guid(EdgeProjectFactory.Guid)]
	public class EdgeProjectFactory : FlavoredProjectFactoryBase
	{
		public const string Guid = "9dd842df-7c77-4e49-a96c-878118a1af27";

		EdgeProjectPackage _package;

		public EdgeProjectFactory(EdgeProjectPackage package)
		{
			_package = package;
		}

		protected override object PreCreateForOuter(IntPtr outerProjectIUnknown)
		{
			return new EdgeServiceLibraryProject(_package);
		}
	}
}
