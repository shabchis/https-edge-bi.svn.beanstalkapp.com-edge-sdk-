// Guids.cs
// MUST match guids.h
using System;

namespace Edge.SDK.VisualStudio
{
    public static class GuidList
    {
        public const string guidEdgeProjectPackagePkgString = EdgeProjectPackage.Guid;
        public const string guidEdgeProjectPackageCmdSetString = "fb17a1b5-0386-4fcc-bf0b-a1fae160115a";

        public static readonly Guid guidEdgeProjectPackageCmdSet = new Guid(guidEdgeProjectPackageCmdSetString);
    };
}