using Cuteribs.CopilotPrReviewer.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cuteribs.CopilotPrReviewer.Tests;

public class FindingParserTests
{
    private readonly FindingParser _parser = new(NullLogger.Instance);

    [Fact]
    public void Parse_ValidJsonArray_ReturnsFindings()
    {
        var json = """
        [
          {
            "filePath": "/src/Service.cs",
            "lineNumber": 42,
            "severity": "Major",
            "description": "Missing null check",
            "suggestion": "Add null guard"
          }
        ]
        """;

        var findings = _parser.Parse(json);

        Assert.Single(findings);
        Assert.Equal("/src/Service.cs", findings[0].FilePath);
        Assert.Equal(42, findings[0].LineNumber);
        Assert.Equal("Major", findings[0].Severity);
        Assert.Equal("Missing null check", findings[0].Description);
        Assert.Equal("Add null guard", findings[0].Suggestion);
    }

    [Fact]
    public void Parse_MarkdownWrappedJson_ReturnsFindings()
    {
        var response = """
        Here are the findings:

        ```json
        [
          {
            "filePath": "/src/Controller.cs",
            "lineNumber": 10,
            "severity": "Critical",
            "description": "SQL injection"
          }
        ]
        ```

        That's all.
        """;

        var findings = _parser.Parse(response);

        Assert.Single(findings);
        Assert.Equal("Critical", findings[0].Severity);
    }

    [Fact]
    public void Parse_EmptyArray_ReturnsEmpty()
    {
        var findings = _parser.Parse("[]");
        Assert.Empty(findings);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        var findings = _parser.Parse("");
        Assert.Empty(findings);
    }

    [Fact]
    public void Parse_MalformedJson_ReturnsEmpty()
    {
        var findings = _parser.Parse("not json at all");
        Assert.Empty(findings);
    }

    [Fact]
    public void Parse_MultipleFindings_ReturnsAll()
    {
        var json = """
        [
          { "filePath": "/a.cs", "lineNumber": 1, "severity": "Major", "description": "Issue 1" },
          { "filePath": "/b.cs", "lineNumber": 2, "severity": "Minor", "description": "Issue 2" }
        ]
        """;

        var findings = _parser.Parse(json);
        Assert.Equal(2, findings.Count);
    }

    [Fact]
    public void ExtractJson_BareCodeBlock_ExtractsJson()
    {
        var text = "Here:\n```\n[{\"a\":1}]\n```\nDone";
        var result = FindingParser.ExtractJson(text);
        Assert.NotNull(result);
        Assert.StartsWith("[", result);
    }

    [Fact]
    public void ExtractJson_NoJson_ReturnsNull()
    {
        var result = FindingParser.ExtractJson("No JSON here at all");
        Assert.Null(result);
    }
}
