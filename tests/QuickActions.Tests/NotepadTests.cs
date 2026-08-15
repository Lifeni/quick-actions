using QuickActions.Notepad;

namespace QuickActions.Tests;

public class SyntaxHighlighterTests
{
    [Theory]
    [InlineData("```csharp\nint x = 1;\n```", "markdown")]
    [InlineData("~~~js\nconst x = 1;\n~~~", "markdown")]
    [InlineData("public static void Main() { }", "csharp")]
    [InlineData("def foo():\n    return 1", "python")]
    [InlineData("const x = 1;", "javascript")]
    [InlineData("hello world", "plain")]
    [InlineData("", "plain")]
    public void DetectLanguage_Detects(string text, string expected)
    {
        Assert.Equal(expected, SyntaxHighlighter.DetectLanguage(text));
    }

    [Fact]
    public void Tokenize_CSharp_HighlightsKeywordStringComment()
    {
        const string text = "public string s = \"abc\"; // 注释";

        var tokens = SyntaxHighlighter.Tokenize(text, "csharp");

        Assert.Contains(tokens, t => t.Type == TokenType.Keyword && text.Substring(t.Start, t.Length) == "public");
        Assert.Contains(tokens, t => t.Type == TokenType.Keyword && text.Substring(t.Start, t.Length) == "string");
        Assert.Contains(tokens, t => t.Type == TokenType.String && text.Substring(t.Start, t.Length) == "\"abc\"");
        Assert.Contains(tokens, t => t.Type == TokenType.Comment && text.Substring(t.Start, t.Length) == "// 注释");
    }

    [Fact]
    public void Tokenize_Markdown_HighlightsFenceAndHeader()
    {
        const string text = "# 标题\n```\nint x = 1;\n```";

        var tokens = SyntaxHighlighter.Tokenize(text, "markdown");

        Assert.Contains(tokens, t => t.Type == TokenType.Header);
        Assert.Contains(tokens, t => t.Type == TokenType.Fence);
    }
}
