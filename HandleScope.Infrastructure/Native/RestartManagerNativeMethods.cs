using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using HandleScope.Domain;

namespace HandleScope.Infrastructure.Native;

internal static class RestartManagerNativeMethods
{
    internal const int ErrorSuccess = 0;
    internal const int ErrorMoreData = 234;

    [DllImport(AppStrings.RestartManagerDllName, CharSet = CharSet.Unicode)]
    internal static extern int RmStartSession(
        out uint sessionHandle,
        int sessionFlags,
        StringBuilder sessionKey);

    [DllImport(AppStrings.RestartManagerDllName, CharSet = CharSet.Unicode)]
    internal static extern int RmRegisterResources(
        uint sessionHandle,
        uint fileCount,
        string[]? fileNames,
        uint applicationCount,
        RmUniqueProcess[]? applications,
        uint serviceCount,
        string[]? serviceNames);

    [DllImport(AppStrings.RestartManagerDllName)]
    internal static extern int RmGetList(
        uint sessionHandle,
        out uint processInfoNeeded,
        ref uint processInfoCount,
        [In, Out] RmProcessInfo[]? affectedApplications,
        ref uint rebootReasons);

    [DllImport(AppStrings.RestartManagerDllName)]
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

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        internal string ApplicationName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
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
