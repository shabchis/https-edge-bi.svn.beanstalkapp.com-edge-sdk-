using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Flavor;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

namespace Edge.SDK.VisualStudio
{
	public class EdgeServiceLibraryProject : FlavoredProjectBase//, IVsGetCfgProvider

	{
		EdgeProjectPackage _package;

		public EdgeServiceLibraryProject(EdgeProjectPackage package)
		{
			_package = package;
		}

		protected override void SetInnerProject(IntPtr innerIUnknown)
		{
			if (this.serviceProvider == null)
				this.serviceProvider = this._package;

			base.SetInnerProject(innerIUnknown);
		}

		protected override void InitializeForOuter(string fileName, string location, string name, uint flags, ref Guid guidProject, out bool cancel)
		{
			base.InitializeForOuter(fileName, location, name, flags, ref guidProject, out cancel);
		}

		/*
		int IVsGetCfgProvider.GetCfgProvider(out IVsCfgProvider ppCfgProvider)
		{
			ppCfgProvider = new DebugConfigProvider();
			return VSConstants.S_OK;
		}
		*/
	}



}
