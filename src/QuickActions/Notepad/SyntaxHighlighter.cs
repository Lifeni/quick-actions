using System.Text.RegularExpressions;

namespace QuickActions.Notepad;

/// <summary>高亮片段类型。</summary>
internal enum TokenType
{
    Comment,
    String,
    Keyword,
    Number,
    Fence,  // Markdown 代码围栏
    Header, // Markdown 标题
}

/// <summary>
/// 极简语法高亮：语言检测 + 规则着色。纯函数，便于测试。
/// 语言：markdown（检测代码围栏 ``` / ~~~）、csharp / python / javascript（关键词启发式）、plain。
/// </summary>
internal static class SyntaxHighlighter
{
    private static readonly string[] CSharpKeywords =
    {
        "using", "public", "private", "protected", "internal", "static", "void", "int", "string",
        "bool", "double", "class", "namespace", "return", "new", "var", "if", "else", "for",
        "foreach", "while", "do", "switch", "case", "break", "continue", "try", "catch", "finally",
        "throw", "null", "true", "false", "this", "base", "readonly", "async", "await", "interface",
        "enum", "struct", "record", "sealed", "override", "virtual", "get", "set",
    };

    private static readonly string[] PythonKeywords =
    {
        "def", "return", "if", "elif", "else", "for", "while", "import", "from", "as", "class",
        "try", "except", "finally", "with", "lambda", "not", "and", "or", "in", "is", "None",
        "True", "False", "pass", "break", "continue", "global", "yield", "async", "await", "self",
    };

    private static readonly string[] JavaScriptKeywords =
    {
        "function", "return", "if", "else", "for", "while", "do", "switch", "case", "break",
        "continue", "new", "var", "let", "const", "class", "import", "export", "from", "async",
        "await", "try", "catch", "finally", "throw", "null", "undefined", "true", "false", "this",
    };

    // 注释 / 字符串 / 数字 / 标识符 统一扫描（Singleline 让 /* */ 可跨行）
    private static readonly Regex TokenRegex = new(
        @"//[^\r\n]*|/\*.*?\*/|#[^\r\n]*|""(?:[^""\\\r\n]|\\.)*""|'(?:[^'\\\r\n]|\\.)*'|`(?:[^`\\\r\n]|\\.)*`|\b\d+(?:\.\d+)?\b|\b[A-Za-z_]\w*\b",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex HeaderRegex = new(@"^#{1,6}\s", RegexOptions.Compiled);

    /// <summary>识别语言名：markdown / csharp / python / javascript / plain。</summary>
    public static string DetectLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "plain";
        if (text.Contains("```") || text.Contains("~~~"))
            return "markdown";

        int cs = CountKeywords(text, CSharpKeywords);
        int py = CountKeywords(text, PythonKeywords);
        int js = CountKeywords(text, JavaScriptKeywords);
        int max = Math.Max(cs, Math.Max(py, js));
        if (max == 0)
            return "plain";
        if (max == cs)
            return "csharp";
        if (max == py)
            return "python";
        return "javascript";
    }

    /// <summary>返回待着色片段（起点、长度、类型）；普通标识符与空白不返回。</summary>
    public static List<(int Start, int Length, TokenType Type)> Tokenize(string text, string language)
    {
        var tokens = new List<(int, int, TokenType)>();
        if (string.IsNullOrEmpty(text))
            return tokens;

        if (language == "markdown")
        {
            TokenizeMarkdown(text, tokens);
            return tokens;
        }

        string[]? keywords = language switch
        {
            "csharp" => CSharpKeywords,
            "python" => PythonKeywords,
            "javascript" => JavaScriptKeywords,
            _ => null,
        };

        foreach (Match m in TokenRegex.Matches(text))
        {
            TokenType? type = Classify(m.Value, keywords);
            if (type is not null)
                tokens.Add((m.Index, m.Length, type.Value));
        }
        return tokens;
    }

    private static void TokenizeMarkdown(string text, List<(int, int, TokenType)> tokens)
    {
        int lineStart = 0;
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            if (line.StartsWith("```") || line.StartsWith("~~~"))
            {
                tokens.Add((lineStart, raw.Length, TokenType.Fence));
            }
            else if (HeaderRegex.IsMatch(line))
            {
                tokens.Add((lineStart, raw.Length, TokenType.Header));
            }
            else
            {
                foreach (Match m in TokenRegex.Matches(raw))
                {
                    TokenType? type = Classify(m.Value, null);
                    if (type is not null)
                        tokens.Add((lineStart + m.Index, m.Length, type.Value));
                }
            }
            lineStart += raw.Length + 1;
        }
    }

    private static TokenType? Classify(string value, string[]? keywords)
    {
        if (value.Length == 0)
            return null;
        char c = value[0];
        if (c == '/' || c == '#')
            return TokenType.Comment;
        if (c == '"' || c == '\'' || c == '`')
            return TokenType.String;
        if (char.IsDigit(c))
            return TokenType.Number;
        return keywords is not null && Array.IndexOf(keywords, value) >= 0 ? TokenType.Keyword : null;
    }

    private static int CountKeywords(string text, string[] keywords)
    {
        int count = 0;
        foreach (string kw in keywords)
        {
            if (Regex.IsMatch(text, @"\b" + Regex.Escape(kw) + @"\b"))
                count++;
        }
        return count;
    }
}
