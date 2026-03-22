using MdView.Services;

namespace MdView.Tests;

public class MarkdownServiceTests
{
    private readonly MarkdownService _service = new();

    [Fact]
    public void ConvertToHtml_BasicMarkdown_ProducesHtml()
    {
        var html = _service.ConvertToHtml("# Hello World");
        Assert.Contains("Hello World</h1>", html);
    }

    [Fact]
    public void ConvertToHtml_Bold_ProducesStrong()
    {
        var html = _service.ConvertToHtml("**bold text**");
        Assert.Contains("<strong>bold text</strong>", html);
    }

    [Fact]
    public void ConvertToHtml_CodeBlock_ProducesPreCode()
    {
        var html = _service.ConvertToHtml("```csharp\nvar x = 1;\n```");
        Assert.Contains("<pre>", html);
        Assert.Contains("<code", html);
        Assert.Contains("var x = 1;", html);
    }

    [Fact]
    public void ConvertToHtml_Table_ProducesTableElements()
    {
        var md = "| A | B |\n|---|---|\n| 1 | 2 |";
        var html = _service.ConvertToHtml(md);
        Assert.Contains("<table>", html);
        Assert.Contains("<th>", html);
        Assert.Contains("<td>", html);
    }

    [Fact]
    public void ConvertToHtml_MermaidBlock_PreservesMermaidClass()
    {
        var md = "```mermaid\nflowchart TD\n    A-->B\n```";
        var html = _service.ConvertToHtml(md);
        Assert.Contains("mermaid", html);
        Assert.Contains("flowchart TD", html);
    }

    [Fact]
    public void ConvertToHtml_Link_ProducesAnchor()
    {
        var html = _service.ConvertToHtml("[click](https://example.com)");
        Assert.Contains("<a href=\"https://example.com\"", html);
        Assert.Contains("click</a>", html);
    }

    [Fact]
    public void ConvertToHtml_TaskList_ProducesCheckboxes()
    {
        var md = "- [x] Done\n- [ ] Todo";
        var html = _service.ConvertToHtml(md);
        Assert.Contains("checked", html);
        Assert.Contains("input", html);
    }

    [Fact]
    public void BuildFullHtml_ProducesCompleteDocument()
    {
        var html = _service.BuildFullHtml("# Test");
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<html>", html);
        Assert.Contains("</html>", html);
        Assert.Contains("Test</h1>", html);
    }

    [Fact]
    public void BuildFullHtml_LightMode_UsesLightColors()
    {
        var html = _service.BuildFullHtml("# Test", darkMode: false);
        Assert.Contains("#ffffff", html); // light background
        Assert.Contains("#24292f", html); // dark text
    }

    [Fact]
    public void BuildFullHtml_DarkMode_UsesDarkColors()
    {
        var html = _service.BuildFullHtml("# Test", darkMode: true);
        Assert.Contains("#1e1e1e", html); // dark background
        Assert.Contains("#d4d4d4", html); // light text
    }
}
