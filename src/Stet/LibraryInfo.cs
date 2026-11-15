using System.Reflection;

namespace Stet;

/// <summary>Version information for the library and its embedded data.</summary>
public static class LibraryInfo
{
    /// <summary>Version of the embedded data format. Changes when data files are not readable by older code.</summary>
    public const int DataFormatVersion = 1;

    /// <summary>Version of the library code, for example "0.1.0".</summary>
    public static string CodeVersion { get; } = ReadVersion();

    private static string ReadVersion()
    {
        string? version = typeof(LibraryInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (version is null)
        {
            return "0.0.0";
        }

        int plus = version.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 ? version : version[..plus];
    }
}
