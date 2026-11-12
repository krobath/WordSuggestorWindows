using System.Runtime.InteropServices;

internal static class UpdatePackageActivation
{
    public static int Start(string appId, string runId)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(appId, @"^WordSuggestor\.Windows\.LocalTest\.[a-f0-9]{8,32}_[a-z0-9]{13}!App$") ||
            !System.Text.RegularExpressions.Regex.IsMatch(runId, "^[a-f0-9]{32}$")) return 2;
        var manager = (IActivation)Activator.CreateInstance(Type.GetTypeFromCLSID(new("45BA127D-10A8-46EA-8AB7-56EA9078943C"))!)!;
        try { Marshal.ThrowExceptionForHR(manager.ActivateApplication(appId, "--qualify-update-policy " + runId, 0, out _)); return 0; }
        catch (COMException) { return 3; }
        finally { Marshal.FinalReleaseComObject(manager); }
    }
    [ComImport, Guid("2E941141-7F97-4756-BA1D-9DECDE894A3D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IActivation
    {
        [PreserveSig] int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)] string appId, [MarshalAs(UnmanagedType.LPWStr)] string arguments, uint options, out uint processId);
        [PreserveSig] int ActivateForFile(IntPtr app, IntPtr items, IntPtr verb, out uint processId);
        [PreserveSig] int ActivateForProtocol(IntPtr app, IntPtr items, out uint processId);
    }
}
