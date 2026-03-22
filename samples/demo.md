# MdView Demo

Welcome to **MdView** — a cross-platform Markdown viewer with Mermaid diagram support.

## Features

- Markdown rendering with GitHub-flavored styling
- Mermaid diagram support
- PDF and XPS export
- Live file watching (auto-reload on save)
- Drag-and-drop file opening
- Dark mode toggle
- Platform-native styling (macOS / Windows)

## Code Example

```csharp
public class HelloWorld
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, MdView!");
    }
}
```

## Table Example

| Feature | macOS | Windows |
|---------|-------|---------|
| Native menu bar | Yes | No (in-window) |
| Title bar style | Extended | Standard |
| Theme | macOS HIG | Fluent |
| XPS Export | No | Yes |

## Mermaid Flowchart

```mermaid
flowchart TD
    A[Open Markdown File] --> B{Parse with Markdig}
    B --> C[Convert to HTML]
    C --> D[Inject into Template]
    D --> E[Render in WebView]
    E --> F{Export?}
    F -->|PDF| G[PuppeteerSharp]
    F -->|XPS| H[Windows Print]
    F -->|No| I[View in App]
```

## Mermaid Sequence Diagram

```mermaid
sequenceDiagram
    participant User
    participant MdView
    participant Markdig
    participant WebView
    participant Puppeteer

    User->>MdView: Open .md file
    MdView->>Markdig: Parse markdown
    Markdig-->>MdView: HTML content
    MdView->>WebView: Load HTML + mermaid.js
    WebView-->>User: Rendered view

    User->>MdView: Export to PDF
    MdView->>Puppeteer: Send HTML
    Puppeteer-->>MdView: PDF file
    MdView-->>User: Save dialog
```

## Blockquote

> "The best way to predict the future is to invent it."
> — Alan Kay

## Task List

- [x] Markdown rendering
- [x] Mermaid diagrams
- [x] PDF export
- [x] XPS export (Windows)
- [x] Dark mode
- [x] File watching
- [x] Drag and drop

---

*Built with Avalonia UI, .NET 10, and Markdig*
