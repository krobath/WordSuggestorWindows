using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public interface IAccountSecretStore { byte[]? Read(string slot); void Write(string slot, byte[] data); void Delete(string slot); }

public sealed class WindowsAccountCredentials(string scope) : IAccountSecretStore
{
    private readonly string _prefix = "WordSuggestor/account/v1/" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scope))) + "/";
    public byte[]? Read(string slot)
    {
        if (!CredReadW(_prefix + slot, 1, 0, out var pointer))
        { if (Marshal.GetLastWin32Error() == 1168) return null; throw new AccountFailure("credential_store"); }
        try
        {
            var value = Marshal.PtrToStructure<Credential>(pointer);
            if (value.Size > 2560) throw new AccountFailure("credential_store");
            var data = new byte[value.Size]; Marshal.Copy(value.Blob, data, 0, data.Length); return data;
        }
        finally { CredFree(pointer); }
    }
    public void Write(string slot, byte[] data)
    {
        if (data.Length > 2560) throw new AccountFailure("credential_store");
        var pointer = Marshal.AllocHGlobal(data.Length);
        try
        {
            Marshal.Copy(data, 0, pointer, data.Length);
            var value = new Credential { Type = 1, Target = _prefix + slot, Size = (uint)data.Length, Blob = pointer, Persist = 2, User = "WordSuggestor" };
            if (!CredWriteW(ref value, 0)) throw new AccountFailure("credential_store");
        }
        finally { Marshal.Copy(new byte[data.Length], 0, pointer, data.Length); Marshal.FreeHGlobal(pointer); }
    }
    public void Delete(string slot)
    { if (!CredDeleteW(_prefix + slot, 1, 0) && Marshal.GetLastWin32Error() != 1168) throw new AccountFailure("credential_store"); }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Credential
    {
        public uint Flags, Type; public string Target; public string? Comment; public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint Size; public IntPtr Blob; public uint Persist, AttributeCount; public IntPtr Attributes; public string? Alias; public string User;
    }
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CredReadW(string target, uint type, uint flags, out IntPtr credential);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CredWriteW(ref Credential credential, uint flags);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CredDeleteW(string target, uint type, uint flags);
    [DllImport("advapi32.dll")] private static extern void CredFree(IntPtr buffer);
}
