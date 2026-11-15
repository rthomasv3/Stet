namespace Stet.Tests;

public class LibraryInfoTests
{
    [Fact]
    public void CodeVersionIsSemanticVersion()
    {
        string[] parts = LibraryInfo.CodeVersion.Split('-')[0].Split('.');

        Assert.Equal(3, parts.Length);
        Assert.All(parts, part => Assert.True(int.TryParse(part, out _)));
    }

    [Fact]
    public void LibraryHasNoRuntimeDependencies()
    {
        string[] allowed = ["System.", "netstandard", "mscorlib"];
        var references = typeof(LibraryInfo).Assembly.GetReferencedAssemblies();

        Assert.All(references, r => Assert.Contains(allowed, a => r.Name!.StartsWith(a, StringComparison.Ordinal)));
    }
}
