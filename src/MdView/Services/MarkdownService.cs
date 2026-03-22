using Markdig;

namespace MdView.Services;

public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public string ConvertToHtml(string markdown)
    {
        return Markdown.ToHtml(markdown, _pipeline);
    }

    public string BuildFullHtml(string markdown, bool darkMode = false)
    {
        var bodyHtml = ConvertToHtml(markdown);
        return HtmlTemplate.Wrap(bodyHtml, darkMode);
    }
}
