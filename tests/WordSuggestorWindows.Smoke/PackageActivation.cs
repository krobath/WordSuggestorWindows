using System.Diagnostics;
using System.Runtime.InteropServices;

internal static class PackageActivation
{
    [ComImport, Guid("2E941141-7F97-4756-BA1D-9DECDE894A3D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IActivationManager
    {
        [PreserveSig] int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)] string appId,
            [MarshalAs(UnmanagedType.LPWStr)] string arguments, uint options, out uint processId);
        [PreserveSig] int ActivateForFile([MarshalAs(UnmanagedType.LPWStr)] string appId, IntPtr items,
            [MarshalAs(UnmanagedType.LPWStr)] string verb, out uint processId);
        [PreserveSig] int ActivateForProtocol([MarshalAs(UnmanagedType.LPWStr)] string appId, IntPtr items, out uint processId);
    }

    public static Process Start(string appId, string arguments)
        => Process.GetProcessById(Activate(appId, arguments));

    public static int Activate(string appId, string arguments)
    {
        var type = Type.GetTypeFromCLSID(new Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C"), throwOnError: true)!;
        var manager = (IActivationManager)Activator.CreateInstance(type)!;
        try
        {
            Marshal.ThrowExceptionForHR(manager.ActivateApplication(appId, arguments, 0, out var pid));
            return (int)pid;
        }
        finally { Marshal.ReleaseComObject(manager); }
    }
}
