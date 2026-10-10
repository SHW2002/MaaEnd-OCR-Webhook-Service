namespace MaaEnd_Log_Retransmitter.Datasets;

internal static class DatasetPaths
{
    public static string DefaultRecordingDirectory => Path.Combine(AppContext.BaseDirectory, "records");
    public static string Display(string path) => Path.GetRelativePath(AppContext.BaseDirectory, Resolve(path));

    public static string Resolve(string path)
        => Path.GetFullPath(path.Trim().Trim('"'), AppContext.BaseDirectory);

    public static string BrowseDirectory(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            try
            {
                var absolute = Resolve(path);
                if (string.Equals(absolute, DefaultRecordingDirectory, StringComparison.OrdinalIgnoreCase))
                    Directory.CreateDirectory(absolute);
                if (Directory.Exists(absolute)) return absolute;
                var parent = Path.GetDirectoryName(absolute);
                if (parent != null && Directory.Exists(parent)) return parent;
            }
            catch (ArgumentException) { }
            catch (NotSupportedException) { }
            catch (PathTooLongException) { }
        }
        Directory.CreateDirectory(DefaultRecordingDirectory);
        return DefaultRecordingDirectory;
    }
}
