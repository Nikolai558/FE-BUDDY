using System;
using System.Diagnostics;
using System.Reflection;

using FeBuddyWPF.Contracts.Services;

namespace FeBuddyWPF.Services;

public class ApplicationInfoService : IApplicationInfoService
{
    public ApplicationInfoService()
    {
    }

    public Version GetVersion()
    {
        // Set the app version in WPF_Template > Properties > Package > PackageVersion
        string assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var version = FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;
        return new Version(version);
    }
}
