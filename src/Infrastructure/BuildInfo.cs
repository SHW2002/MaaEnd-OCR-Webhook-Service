using System.Reflection;

namespace MaaEnd_Log_Retransmitter.Infrastructure;

internal static class BuildInfo
{
    public static string Version => typeof(BuildInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
    public static string Id => typeof(BuildInfo).Assembly.ManifestModule.ModuleVersionId.ToString();
}
