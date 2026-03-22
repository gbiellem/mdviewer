using MdView.ViewModels;

namespace MdView.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void InitialState_HasNoFile()
    {
        var vm = new MainWindowViewModel();
        Assert.False(vm.HasFile);
        Assert.Equal("MdView", vm.WindowTitle);
        Assert.Equal("Ready", vm.StatusText);
        Assert.Equal(string.Empty, vm.HtmlContent);
    }

    [Fact]
    public void LoadFile_SetsHasFile()
    {
        var vm = new MainWindowViewModel();
        var tempFile = CreateTempMarkdown("# Test");

        vm.LoadFile(tempFile);

        Assert.True(vm.HasFile);
        File.Delete(tempFile);
    }

    [Fact]
    public void LoadFile_SetsWindowTitle()
    {
        var vm = new MainWindowViewModel();
        var tempFile = CreateTempMarkdown("# Test");

        vm.LoadFile(tempFile);

        Assert.Contains(Path.GetFileName(tempFile), vm.WindowTitle);
        Assert.Contains("MdView", vm.WindowTitle);
        File.Delete(tempFile);
    }

    [Fact]
    public void LoadFile_SetsHtmlContent()
    {
        var vm = new MainWindowViewModel();
        var tempFile = CreateTempMarkdown("# Hello World");

        vm.LoadFile(tempFile);

        Assert.NotEmpty(vm.HtmlContent);
        Assert.Contains("Hello World", vm.HtmlContent);
        Assert.Contains("<!DOCTYPE html>", vm.HtmlContent);
        File.Delete(tempFile);
    }

    [Fact]
    public void LoadFile_SetsStatusText()
    {
        var vm = new MainWindowViewModel();
        var tempFile = CreateTempMarkdown("# Test");

        vm.LoadFile(tempFile);

        Assert.Contains("Loaded", vm.StatusText);
        File.Delete(tempFile);
    }

    [Fact]
    public void LoadFile_SetsCurrentFilePath()
    {
        var vm = new MainWindowViewModel();
        var tempFile = CreateTempMarkdown("# Test");

        vm.LoadFile(tempFile);

        Assert.Equal(tempFile, vm.CurrentFilePath);
        File.Delete(tempFile);
    }

    [Fact]
    public void LoadFile_NonExistentFile_DoesNothing()
    {
        var vm = new MainWindowViewModel();

        vm.LoadFile("/nonexistent/path/file.md");

        Assert.False(vm.HasFile);
        Assert.Equal(string.Empty, vm.HtmlContent);
    }

    [Fact]
    public void LoadFile_CalledTwice_UpdatesContent()
    {
        var vm = new MainWindowViewModel();
        var tempFile = CreateTempMarkdown("# Version 1");
        vm.LoadFile(tempFile);
        Assert.Contains("Version 1", vm.HtmlContent);

        // Modify file on disk and reload
        File.WriteAllText(tempFile, "# Version 2");
        vm.LoadFile(tempFile);

        Assert.Contains("Version 2", vm.HtmlContent);
        File.Delete(tempFile);
    }

    [Fact]
    public void DarkMode_Toggle_UpdatesHtmlContent()
    {
        var vm = new MainWindowViewModel();
        var tempFile = CreateTempMarkdown("# Test");
        vm.LoadFile(tempFile);

        var lightHtml = vm.HtmlContent;
        vm.IsDarkMode = true;
        var darkHtml = vm.HtmlContent;

        Assert.NotEqual(lightHtml, darkHtml);
        Assert.Contains("#1e1e1e", darkHtml);
        Assert.Contains("#ffffff", lightHtml);
        File.Delete(tempFile);
    }

    [Fact]
    public void LoadFile_WithMermaid_ContainsMermaidInHtml()
    {
        var md = "# Test\n\n```mermaid\nflowchart TD\n    A-->B\n```";
        var vm = new MainWindowViewModel();
        var tempFile = CreateTempMarkdown(md);

        vm.LoadFile(tempFile);

        Assert.Contains("mermaid", vm.HtmlContent);
        Assert.Contains("flowchart TD", vm.HtmlContent);
        File.Delete(tempFile);
    }

    [Fact]
    public void LoadFromArgs_LoadsFirstMarkdownFile()
    {
        var vm = new MainWindowViewModel();
        var tempFile = CreateTempMarkdown("# From Args");

        vm.LoadFromArgs(["--some-flag", tempFile]);

        Assert.True(vm.HasFile);
        Assert.Contains("From Args", vm.HtmlContent);
        File.Delete(tempFile);
    }

    [Fact]
    public void LoadFromArgs_NoValidFiles_DoesNothing()
    {
        var vm = new MainWindowViewModel();

        vm.LoadFromArgs(["--flag", "/nonexistent.md"]);

        Assert.False(vm.HasFile);
    }

    [Fact]
    public void IsXpsAvailable_MatchesPlatform()
    {
        var vm = new MainWindowViewModel();
        Assert.Equal(OperatingSystem.IsWindows(), vm.IsXpsAvailable);
    }

    private static string CreateTempMarkdown(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"mdview_test_{Guid.NewGuid():N}.md");
        File.WriteAllText(path, content);
        return path;
    }
}
