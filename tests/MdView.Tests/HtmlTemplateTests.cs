using MdView.Services;

namespace MdView.Tests;

public class HtmlTemplateTests
{
    [Fact]
    public void Wrap_ProducesValidHtmlDocument()
    {
        var html = HtmlTemplate.Wrap("<p>Hello</p>");
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<html>", html);
        Assert.Contains("</html>", html);
        Assert.Contains("<p>Hello</p>", html);
    }

    [Fact]
    public void Wrap_LightMode_HasCorrectBackground()
    {
        var html = HtmlTemplate.Wrap("<p>Test</p>", darkMode: false);
        Assert.Contains("#ffffff", html);
    }

    [Fact]
    public void Wrap_DarkMode_HasCorrectBackground()
    {
        var html = HtmlTemplate.Wrap("<p>Test</p>", darkMode: true);
        Assert.Contains("#1e1e1e", html);
    }

    [Fact]
    public void Wrap_DarkMode_MermaidThemeIsDark()
    {
        var html = HtmlTemplate.Wrap("<p>Test</p>", darkMode: true);
        // mermaid script only present when mermaid.min.js is bundled
        if (html.Contains("mermaid.initialize"))
            Assert.Contains("theme: 'dark'", html);
    }

    [Fact]
    public void Wrap_LightMode_MermaidThemeIsDefault()
    {
        var html = HtmlTemplate.Wrap("<p>Test</p>", darkMode: false);
        if (html.Contains("mermaid.initialize"))
            Assert.Contains("theme: 'default'", html);
    }

    [Fact]
    public void Wrap_WithoutMermaidJs_StillProducesValidHtml()
    {
        // In test context, mermaid.min.js may not be present
        var html = HtmlTemplate.Wrap("<p>Test</p>");
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<p>Test</p>", html);
    }

    [Fact]
    public void Wrap_IncludesMarkdownBodyDiv()
    {
        var html = HtmlTemplate.Wrap("<h1>Title</h1>");
        Assert.Contains("<div class=\"markdown-body\">", html);
        Assert.Contains("<h1>Title</h1>", html);
    }

    [Fact]
    public void Wrap_IncludesResponsiveViewport()
    {
        var html = HtmlTemplate.Wrap("<p>Test</p>");
        Assert.Contains("viewport", html);
    }
}
