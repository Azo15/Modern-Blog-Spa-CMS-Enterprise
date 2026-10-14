using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace BusinessLayer.Security;

/// <summary>Blocks filesystem redirection without rejecting non-redirecting cloud placeholders.</summary>
public static class StoragePathSecurity
{
    public static void RejectRedirect(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            FileSystemInfo info = Directory.Exists(path) ? new DirectoryInfo(path) : new FileInfo(path);
            if (info.LinkTarget is not null) throw new UnauthorizedAccessException("Linked image paths are not allowed.");
            return;
        }
        if (!File.Exists(path) && !Directory.Exists(path)) return;
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0) return;
        // OneDrive uses non-surrogate reparse tags. Junctions and symlinks have
        // the name-surrogate bit; inspect the point itself, never its target.
        using var handle = CreateFile(path, 0x80, 7, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
        if (handle.IsInvalid || !GetFileInformationByHandleEx(handle, 9, out var tag, 8))
            throw new UnauthorizedAccessException("Image path metadata could not be validated.");
        if ((tag.ReparseTag & 0x20000000) != 0)
            throw new UnauthorizedAccessException("Linked image paths are not allowed.");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AttributeTagInfo { public uint Attributes; public uint ReparseTag; }
    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandleEx(SafeFileHandle handle, int informationClass, out AttributeTagInfo information, uint size);
}
