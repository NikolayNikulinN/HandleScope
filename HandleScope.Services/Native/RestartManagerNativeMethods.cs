using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using HandleScope.Services.Constants;

namespace HandleScope.Services.Native;

internal static class RestartManagerNativeMethods
{
    [DllImport(ServiceConstants.RestartManagerDllName, CharSet = CharSet.Unicode)]
    internal static extern int RmStartSession(
        out uint sessionHandle,
        int sessionFlags,
        StringBuilder sessionKey);

    [DllImport(ServiceConstants.RestartManagerDllName, CharSet = CharSet.Unicode)]
    internal static extern int RmRegisterResources(
        uint sessionHandle,
        uint fileCount,
        string[]? fileNames,
        uint applicationCount,
        RmUniqueProcess[]? applications,
        uint serviceCount,
        string[]? serviceNames);

    [DllImport(ServiceConstants.RestartManagerDllName)]
    internal static extern int RmGetList(
        uint sessionHandle,
        out uint processInfoNeeded,
        ref uint processInfoCount,
        [In, Out] RmProcessInfo[]? affectedApplications,
        ref uint rebootReasons);

    [DllImport(ServiceConstants.RestartManagerDllName)]
    internal static extern int RmEndSession(uint sessionHandle);

    [StructLayout(LayoutKind.Sequential)]
    internal struct RmUniqueProcess
    {
        internal int ProcessId;
        internal FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct RmProcessInfo
    {
        internal RmUniqueProcess Process;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ServiceConstants.ApplicationNameCapacity)]
        internal string ApplicationName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ServiceConstants.ServiceNameCapacity)]
        internal string ServiceShortName;

        internal RmApplicationType ApplicationType;
        internal uint ApplicationStatus;
        internal uint TerminalServicesSessionId;

        [MarshalAs(UnmanagedType.Bool)]
        internal bool Restartable;
    }

    internal enum RmApplicationType
    {
        Unknown = 0,
        MainWindow = 1,
        OtherWindow = 2,
        Service = 3,
        Explorer = 4,
        Console = 5,
        Critical = 1000
    }
}
