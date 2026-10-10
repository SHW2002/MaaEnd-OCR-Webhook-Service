using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MaaEnd_Log_Retransmitter.Datasets;

internal sealed class PendingRecording
{
    [DllImport("kernel32.dll", EntryPoint = "CreateDirectoryW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateDirectoryExclusive(string path, IntPtr securityAttributes);

    private readonly string _parentDirectory;
    private bool _resolved;
    public string DirectoryPath { get; }
    public string SavedDirectory { get; }
    public int FrameCount => Directory.Exists(Path.Combine(DirectoryPath, "frames"))
        ? Directory.EnumerateFiles(Path.Combine(DirectoryPath, "frames"), "*.png").Count() : 0;

    private PendingRecording(string parentDirectory)
    {
        _parentDirectory = Path.TrimEndingDirectorySeparator(DatasetPaths.Resolve(parentDirectory));
        (DirectoryPath, SavedDirectory) = CreateSession(_parentDirectory);
    }

    public static PendingRecording Create(string parentDirectory) => new(parentDirectory);

    private static (string Pending, string Saved) CreateSession(string parent)
    {
        Directory.CreateDirectory(parent);
        var stem = $"session-{DateTime.Now:yyyyMMdd-HHmmss}";
        for (var number = 1; ; number++)
        {
            var name = number == 1 ? stem : $"{stem}-{number}";
            var pending = Path.Combine(parent, ".pending-" + name);
            var saved = Path.Combine(parent, name);
            if (Path.Exists(saved)) continue;
            // Unlike Directory.CreateDirectory, the native call fails if another recorder owns this name.
            if (!CreateDirectoryExclusive(pending, IntPtr.Zero))
            {
                var error = Marshal.GetLastWin32Error();
                if (error is 183 or 80) continue;
                throw new IOException("无法创建录制会话目录", new Win32Exception(error));
            }
            // A previous pending session could have been saved between the check and creation.
            if (Path.Exists(saved))
            {
                Directory.Delete(pending);
                continue;
            }
            return (pending, saved);
        }
    }

    public string Save()
    {
        EnsurePending();
        var manifest = new DatasetStore(DirectoryPath).LoadManifest();
        if (manifest.FinishedUtc == null) throw new InvalidOperationException("请先停止录制再保存");
        Directory.Move(DirectoryPath, SavedDirectory);
        _resolved = true;
        return SavedDirectory;
    }

    public void Delete()
    {
        EnsurePending();
        Directory.Delete(DirectoryPath, recursive: true);
        _resolved = true;
    }

    private void EnsurePending()
    {
        if (_resolved) throw new InvalidOperationException("本次录制已保存或删除");
        // Only this newly created session can be removed; never the output folder or a saved session.
        var absolutePath = Path.GetFullPath(DirectoryPath);
        var parent = Path.GetDirectoryName(absolutePath);
        if (!string.Equals(parent, _parentDirectory, StringComparison.OrdinalIgnoreCase)
            || !Path.GetFileName(absolutePath).StartsWith(".pending-session-", StringComparison.Ordinal)
            || (File.GetAttributes(absolutePath) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("待处理录制目录无效");
    }
}
