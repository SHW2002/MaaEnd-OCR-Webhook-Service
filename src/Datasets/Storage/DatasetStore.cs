using System.Text.Json;
using MaaEnd_Log_Retransmitter.App;

namespace MaaEnd_Log_Retransmitter.Datasets;

internal sealed class DatasetStore(string directory)
{
    public string DirectoryPath { get; } = Path.GetFullPath(directory);
    public string ManifestPath => Path.Combine(DirectoryPath, "dataset.json");
    public string AnnotationsPath => Path.Combine(DirectoryPath, "annotations.json");
    public static DatasetStore Open(string path)
        => new(File.Exists(path) ? Path.GetDirectoryName(Path.GetFullPath(path))! : path);

    public DatasetManifest LoadManifest()
    {
        var manifest = Read<DatasetManifest>(ManifestPath);
        if (manifest.SchemaVersion != 1 || manifest.Format != "lossless-png-sequence")
            throw new InvalidDataException("不支持的数据集格式");
        if (manifest.Frames.Count == 0) throw new InvalidDataException("数据集没有可用帧");
        if (manifest.Frames.Select(f => f.Index).Distinct().Count() != manifest.Frames.Count)
            throw new InvalidDataException("数据集帧编号重复");
        double lastOffset = -1;
        foreach (var frame in manifest.Frames)
        {
            if (!double.IsFinite(frame.OffsetMs) || frame.OffsetMs < lastOffset || frame.Width <= 0 || frame.Height <= 0)
                throw new InvalidDataException("数据集时间戳或尺寸无效");
            lastOffset = frame.OffsetMs;
            if (!File.Exists(FramePath(frame))) throw new FileNotFoundException("数据集帧缺失", frame.File);
        }
        return manifest;
    }

    public DatasetAnnotations LoadAnnotations()
        => File.Exists(AnnotationsPath) ? Read<DatasetAnnotations>(AnnotationsPath) : new();
    public void SaveManifest(DatasetManifest manifest) => Write(ManifestPath, manifest);
    public void SaveAnnotations(DatasetAnnotations annotations) => Write(AnnotationsPath, annotations);
    public string FramePath(DatasetFrame frame)
    {
        var path = Path.GetFullPath(Path.Combine(DirectoryPath, frame.File));
        if (!path.StartsWith(DirectoryPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("数据集图片路径超出会话目录");
        return path;
    }

    private static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path))
        ?? throw new InvalidDataException($"文件为空：{path}");
    public static void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value, ConfigStore.JsonOptions));
        File.Move(temp, path, true);
    }
}
