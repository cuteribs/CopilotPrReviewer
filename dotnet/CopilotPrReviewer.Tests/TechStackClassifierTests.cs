using Cuteribs.CopilotPrReviewer.Models;
using Cuteribs.CopilotPrReviewer.Services;

namespace Cuteribs.CopilotPrReviewer.Tests;

public class TechStackClassifierTests
{
    [Theory]
    [InlineData("/src/Service.cs", TechStack.Dotnet)]
    [InlineData("/src/Project.csproj", TechStack.Dotnet)]
    [InlineData("/Solution.sln", TechStack.Dotnet)]
    [InlineData("/Page.razor", TechStack.Dotnet)]
    [InlineData("/View.cshtml", TechStack.Dotnet)]
    [InlineData("/Directory.Build.props", TechStack.Dotnet)]
    public void Classify_DotnetExtensions_ReturnsDotnet(string path, TechStack expected)
    {
        Assert.Equal(expected, BatchBuilder.ClassifyFile(path));
    }

    [Theory]
    [InlineData("/app.js", TechStack.Frontend)]
    [InlineData("/component.tsx", TechStack.Frontend)]
    [InlineData("/styles.css", TechStack.Frontend)]
    [InlineData("/index.html", TechStack.Frontend)]
    [InlineData("/styles.scss", TechStack.Frontend)]
    [InlineData("/app.vue", TechStack.Frontend)]
    public void Classify_FrontendExtensions_ReturnsFrontend(string path, TechStack expected)
    {
        Assert.Equal(expected, BatchBuilder.ClassifyFile(path));
    }

    [Theory]
    [InlineData("/script.py", TechStack.Python)]
    [InlineData("/types.pyi", TechStack.Python)]
    public void Classify_PythonExtensions_ReturnsPython(string path, TechStack expected)
    {
        Assert.Equal(expected, BatchBuilder.ClassifyFile(path));
    }

    [Theory]
    [InlineData("/config.json", TechStack.Config)]
    [InlineData("/settings.yaml", TechStack.Config)]
    [InlineData("/pipeline.yml", TechStack.Config)]
    public void Classify_ConfigExtensions_ReturnsConfig(string path, TechStack expected)
    {
        Assert.Equal(expected, BatchBuilder.ClassifyFile(path));
    }

    [Theory]
    [InlineData("/readme.md")]
    [InlineData("/image.png")]
    [InlineData("/binary.exe")]
    [InlineData("/data.xml")]
    public void Classify_UnknownExtensions_ReturnsNull(string path)
    {
        Assert.Null(BatchBuilder.ClassifyFile(path));
    }
}
