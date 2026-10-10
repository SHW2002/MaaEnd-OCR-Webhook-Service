using System.Text.RegularExpressions;
using MaaEnd_Log_Retransmitter.Datasets;

namespace MaaEnd_Log_Retransmitter.Tests;

internal static class RecordingSessionTests
{
    public static void Run(string artifacts)
    {
        var output = Path.Combine(artifacts, "recording-naming-" + Guid.NewGuid().ToString("N"));
        var sessions = Enumerable.Range(0, 32).AsParallel().Select(_ => PendingRecording.Create(output)).ToArray();
        Program.Assert(sessions.Select(x => x.SavedDirectory).Distinct(StringComparer.OrdinalIgnoreCase).Count() == sessions.Length,
            "同秒并发创建的录制必须具有不同目录名");
        foreach (var session in sessions)
        {
            var name = Path.GetFileName(session.SavedDirectory);
            Program.Assert(Regex.IsMatch(name, @"^session-\d{8}-\d{6}(?:-[2-9]\d*|-1\d+)?$"),
                "会话名称必须使用日期时间和可选数字后缀");
            Program.Assert(Path.GetFileName(session.DirectoryPath) == ".pending-" + name,
                "保存前后必须使用相同的日期时间会话名");
            File.WriteAllText(Path.Combine(session.DirectoryPath, "retain.txt"), name);
        }
        foreach (var session in sessions)
            Program.Assert(File.ReadAllText(Path.Combine(session.DirectoryPath, "retain.txt")) == Path.GetFileName(session.SavedDirectory),
                "创建其它录制不能覆盖现有待保存会话");
        Program.Assert(!Directory.EnumerateFiles(output, "*.reserve").Any(), "创建后不能残留占位文件");
        foreach (var session in sessions) session.Delete();
        Console.WriteLine("PASS: readable session names and concurrent collision protection");
    }
}
