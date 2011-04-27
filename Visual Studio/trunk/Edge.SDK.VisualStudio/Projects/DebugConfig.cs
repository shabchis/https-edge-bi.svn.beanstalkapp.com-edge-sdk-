using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

namespace Edge.SDK.VisualStudio
{
	class DebugConfig: IVsDebuggableProjectCfg2, IVsQueryDebuggableProjectCfg
	{
		#region IVsDebuggableProjectCfg2 Members

		public int DebugLaunch(uint grfLaunch)
		{
			throw new NotImplementedException();
		}

		public int EnumOutputs(out IVsEnumOutputs ppIVsEnumOutputs)
		{
			throw new NotImplementedException();
		}

		public int OnBeforeDebugLaunch(uint grfLaunch)
		{
			throw new NotImplementedException();
		}

		public int OpenOutput(string szOutputCanonicalName, out IVsOutput ppIVsOutput)
		{
			throw new NotImplementedException();
		}

		public int QueryDebugLaunch(uint grfLaunch, out int pfCanLaunch)
		{
			throw new NotImplementedException();
		}

		public int get_BuildableProjectCfg(out IVsBuildableProjectCfg ppIVsBuildableProjectCfg)
		{
			throw new NotImplementedException();
		}

		public int get_CanonicalName(out string pbstrCanonicalName)
		{
			throw new NotImplementedException();
		}

		public int get_DisplayName(out string pbstrDisplayName)
		{
			throw new NotImplementedException();
		}

		public int get_IsDebugOnly(out int pfIsDebugOnly)
		{
			throw new NotImplementedException();
		}

		public int get_IsPackaged(out int pfIsPackaged)
		{
			throw new NotImplementedException();
		}

		public int get_IsReleaseOnly(out int pfIsReleaseOnly)
		{
			throw new NotImplementedException();
		}

		public int get_IsSpecifyingOutputSupported(out int pfIsSpecifyingOutputSupported)
		{
			throw new NotImplementedException();
		}

		public int get_Platform(out Guid pguidPlatform)
		{
			throw new NotImplementedException();
		}

		public int get_ProjectCfgProvider(out IVsProjectCfgProvider ppIVsProjectCfgProvider)
		{
			throw new NotImplementedException();
		}

		public int get_RootURL(out string pbstrRootURL)
		{
			throw new NotImplementedException();
		}

		public int get_TargetCodePage(out uint puiTargetCodePage)
		{
			throw new NotImplementedException();
		}

		public int get_UpdateSequenceNumber(Microsoft.VisualStudio.OLE.Interop.ULARGE_INTEGER[] puliUSN)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IVsQueryDebuggableProjectCfg Members

		public int QueryDebugTargets(uint grfLaunch, uint cTargets, VsDebugTargetInfo2[] rgDebugTargetInfo, uint[] pcActual = null)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	class DebugConfigProvider : IVsCfgProvider
	{

		public int GetCfgs(uint celt, IVsCfg[] a, uint[] actual, uint[] flags)
		{
			if (flags != null)
				flags[0] = 0;

			int i = 0;

			if (a != null)
			{
				a[0] = new DebugConfig();
			}
			else
				i = 1;

			if (actual != null)
				actual[0] = (uint)i;

			return VSConstants.S_OK;
		}


	}

}
