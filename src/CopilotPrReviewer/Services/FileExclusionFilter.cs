namespace Cuteribs.CopilotPrReviewer.Services;

public sealed class FileExclusionFilter
{
    private static readonly string[] ExactNameExclusions =
    [
        "package-lock.json",
        "yarn.lock",
        "pnpm-lock.yaml",
        "pipfile.lock",
        "poetry.lock",
        "packages.lock.json",
    ];

    private static readonly string[] SuffixExclusions =
    [
        ".min.js",
        ".min.css",
        ".d.ts",
        ".g.cs",
        ".Designer.cs",
        ".generated.cs",
    ];

    public bool ShouldExclude(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        foreach (var name in ExactNameExclusions)
        {
            if (fileName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var suffix in SuffixExclusions)
        {
            if (filePath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
