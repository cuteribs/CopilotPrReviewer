using CopilotPrReviewer.Services;

namespace CopilotPrReviewer.Tests;

public class FileExclusionFilterTests
{
    private readonly FileExclusionFilter _filter = new();

    [Theory]
    [InlineData("/project/package-lock.json")]
    [InlineData("/project/yarn.lock")]
    [InlineData("/project/pnpm-lock.yaml")]
    [InlineData("/project/packages.lock.json")]
    [InlineData("/project/pipfile.lock")]
    [InlineData("/project/poetry.lock")]
    public void ShouldExclude_LockFiles_ReturnsTrue(string path)
    {
        Assert.True(_filter.ShouldExclude(path));
    }

    [Theory]
    [InlineData("/dist/app.min.js")]
    [InlineData("/dist/styles.min.css")]
    [InlineData("/types/index.d.ts")]
    [InlineData("/Generated/Model.g.cs")]
    [InlineData("/Forms/Form1.Designer.cs")]
    [InlineData("/Output/Data.generated.cs")]
    public void ShouldExclude_GeneratedFiles_ReturnsTrue(string path)
    {
        Assert.True(_filter.ShouldExclude(path));
    }

    [Theory]
    [InlineData("/src/Service.cs")]
    [InlineData("/src/app.js")]
    [InlineData("/src/config.json")]
    [InlineData("/src/script.py")]
    [InlineData("/src/styles.css")]
    public void ShouldExclude_NormalFiles_ReturnsFalse(string path)
    {
        Assert.False(_filter.ShouldExclude(path));
    }
}
