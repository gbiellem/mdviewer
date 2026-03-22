namespace MdView.Services;

public static class HtmlTemplate
{
    private static string? _mermaidJs;

    /// <summary>
    /// Loads the bundled mermaid.min.js content once and caches it.
    /// </summary>
    public static string GetMermaidJs()
    {
        if (_mermaidJs != null) return _mermaidJs;

        var appDir = AppContext.BaseDirectory;
        var mermaidPath = Path.Combine(appDir, "Assets", "mermaid.min.js");
        if (File.Exists(mermaidPath))
        {
            _mermaidJs = File.ReadAllText(mermaidPath);
        }
        else
        {
            _mermaidJs = ""; // Graceful fallback if not found
        }

        return _mermaidJs;
    }

    private const string Template = """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <style>
                * { box-sizing: border-box; }

                html, body {
                    margin: 0;
                    padding: 0;
                    background: /*BG_COLOR*/;
                    color: /*TEXT_COLOR*/;
                    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", "Noto Sans", Helvetica, Arial, sans-serif;
                    font-size: 16px;
                    line-height: 1.6;
                    word-wrap: break-word;
                }

                .markdown-body {
                    max-width: 900px;
                    margin: 0 auto;
                    padding: 2rem 2.5rem;
                }

                h1, h2, h3, h4, h5, h6 {
                    margin-top: 1.5em;
                    margin-bottom: 0.5em;
                    font-weight: 600;
                    line-height: 1.25;
                }
                h1 { font-size: 2em; padding-bottom: 0.3em; border-bottom: 1px solid /*BORDER_COLOR*/; }
                h2 { font-size: 1.5em; padding-bottom: 0.3em; border-bottom: 1px solid /*BORDER_COLOR*/; }
                h3 { font-size: 1.25em; }
                h4 { font-size: 1em; }

                p { margin: 0.5em 0 1em; }

                a { color: /*LINK_COLOR*/; text-decoration: none; }
                a:hover { text-decoration: underline; }

                strong { font-weight: 600; }

                code {
                    background: /*CODE_BG*/;
                    border-radius: 6px;
                    padding: 0.2em 0.4em;
                    font-family: ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, "Liberation Mono", monospace;
                    font-size: 85%;
                }

                pre {
                    background: /*CODE_BG*/;
                    border-radius: 6px;
                    padding: 16px;
                    overflow-x: auto;
                    line-height: 1.45;
                    margin: 0.5em 0 1em;
                }

                pre code {
                    background: transparent;
                    padding: 0;
                    font-size: 85%;
                    line-height: inherit;
                }

                blockquote {
                    margin: 0.5em 0 1em;
                    padding: 0 1em;
                    border-left: 4px solid /*BORDER_COLOR*/;
                    color: /*BLOCKQUOTE_TEXT*/;
                }

                ul, ol {
                    padding-left: 2em;
                    margin: 0.5em 0 1em;
                }
                li { margin: 0.25em 0; }

                .task-list-item {
                    list-style-type: none;
                    margin-left: -1.5em;
                }
                .task-list-item input {
                    margin-right: 0.5em;
                }

                table {
                    border-collapse: collapse;
                    width: 100%;
                    margin: 0.5em 0 1em;
                }
                th, td {
                    border: 1px solid /*TABLE_BORDER*/;
                    padding: 8px 13px;
                    text-align: left;
                }
                th {
                    font-weight: 600;
                    background: /*STRIPE_BG*/;
                }
                tr:nth-child(even) {
                    background: /*STRIPE_BG*/;
                }

                hr {
                    border: 0;
                    border-top: 1px solid /*BORDER_COLOR*/;
                    margin: 1.5em 0;
                }

                img {
                    max-width: 100%;
                    height: auto;
                    border-radius: 4px;
                }

                .mermaid {
                    text-align: center;
                    margin: 1em 0;
                }

                @media print {
                    .markdown-body { max-width: none; padding: 0; }
                    pre { white-space: pre-wrap; }
                }
            </style>
            <!--MERMAID_SCRIPT-->
            <script>document.addEventListener('contextmenu', e => e.preventDefault());</script>
        </head>
        <body>
            <div class="markdown-body">
                <!--BODY_HTML-->
            </div>
        </body>
        </html>
        """;

    public static string Wrap(string bodyHtml, bool darkMode = false)
    {
        var mermaidJs = GetMermaidJs();
        var mermaidTheme = darkMode ? "dark" : "default";
        var mermaidScript = string.IsNullOrEmpty(mermaidJs)
            ? ""
            : "<script>" + mermaidJs + "</script>\n" +
              $"<script>mermaid.initialize({{ startOnLoad: true, theme: '{mermaidTheme}', securityLevel: 'loose' }});</script>";

        return Template
            .Replace("/*BG_COLOR*/", darkMode ? "#1e1e1e" : "#ffffff")
            .Replace("/*TEXT_COLOR*/", darkMode ? "#d4d4d4" : "#24292f")
            .Replace("/*CODE_BG*/", darkMode ? "#2d2d2d" : "#f6f8fa")
            .Replace("/*BORDER_COLOR*/", darkMode ? "#444" : "#d0d7de")
            .Replace("/*LINK_COLOR*/", darkMode ? "#58a6ff" : "#0969da")
            .Replace("/*BLOCKQUOTE_TEXT*/", darkMode ? "#8b949e" : "#57606a")
            .Replace("/*TABLE_BORDER*/", darkMode ? "#444" : "#d0d7de")
            .Replace("/*STRIPE_BG*/", darkMode ? "#262626" : "#f6f8fa")
            .Replace("<!--MERMAID_SCRIPT-->", mermaidScript)
            .Replace("<!--BODY_HTML-->", bodyHtml);
    }
}
