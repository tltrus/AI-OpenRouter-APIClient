using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;

namespace AI.Utils
{
    public static class CSharpSyntaxHighlighter
    {
        // Цвета для разных типов токенов
        private static readonly Brush KeywordBrush = Brushes.Blue;
        private static readonly Brush TypeBrush = Brushes.DarkCyan;
        private static readonly Brush StringBrush = Brushes.DarkRed;
        private static readonly Brush NumberBrush = Brushes.DarkOrange;
        private static readonly Brush CommentBrush = Brushes.Green;
        private static readonly Brush PreprocessorBrush = Brushes.DarkMagenta;
        private static readonly Brush OperatorBrush = Brushes.DarkGray;
        private static readonly Brush TextBrush = Brushes.Black;

        // Предкомпилированные регулярные выражения
        private static readonly Regex NumberRegex =
            new Regex(@"\b\d+(\.\d+)?([eE][+-]?\d+)?[fFmMdD]?\b", RegexOptions.Compiled);

        private static readonly Regex StringLiteralRegex =
            new Regex(@"""[^""]*""|'[^']*'", RegexOptions.Compiled);

        private static readonly Regex SingleLineCommentRegex =
            new Regex(@"//.*$", RegexOptions.Compiled);

        private static readonly Regex MultiLineCommentRegex =
            new Regex(@"/\*.*?\*/", RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex PreprocessorRegex =
            new Regex(@"^\s*#.*$", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex IdentifierRegex =
            new Regex(@"\b[A-Za-z_][A-Za-z0-9_]*\b", RegexOptions.Compiled);

        // Ключевые слова C#
        private static readonly HashSet<string> Keywords = new HashSet<string>
        {
            // Пространства имен и модификаторы доступа
            "using", "namespace", "class", "struct", "interface", "enum",
            "public", "private", "protected", "internal", "sealed", "abstract",
            "static", "const", "readonly", "volatile", "virtual", "override",
            "async", "await", "extern", "unsafe", "partial",

            // Типы данных
            "void", "bool", "byte", "sbyte", "char", "decimal", "double",
            "float", "int", "uint", "long", "ulong", "short", "ushort",
            "string", "object", "dynamic", "var",

            // Операторы управления
            "if", "else", "switch", "case", "default", "break", "continue",
            "for", "foreach", "while", "do", "goto", "return", "yield",
            "throw", "try", "catch", "finally", "checked", "unchecked",
            "lock", "fixed", "using", "sizeof", "typeof", "is", "as",
            "new", "this", "base", "value", "out", "ref", "in", "params",
            "operator", "implicit", "explicit", "where", "get", "set",
            "add", "remove", "event", "delegate", "stackalloc"
        };

        // Встроенные типы и классы
        private static readonly HashSet<string> BuiltInTypes = new HashSet<string>
        {
            "Console", "Math", "String", "Int32", "Double", "Single", "Decimal",
            "Boolean", "Char", "Byte", "SByte", "Int16", "UInt16", "Int64", "UInt64",
            "Object", "Array", "List", "Dictionary", "HashSet", "Queue", "Stack",
            "Tuple", "ValueTuple", "Task", "Task<>", "ValueTask", "DateTime",
            "TimeSpan", "Guid", "Regex", "StringBuilder", "Stream", "File",
            "Directory", "Path", "Environment", "Convert", "Activator"
        };

        /// <summary>
        /// Форматирует код C# с подсветкой синтаксиса
        /// </summary>
        public static Paragraph FormatCode(string code)
        {
            var paragraph = new Paragraph
            {
                Margin = new System.Windows.Thickness(0),
                FontFamily = new FontFamily("Consolas, 'Cascadia Code', monospace"),
                FontSize = 11.5,
                LineHeight = 1.1
            };

            var lines = code.Split(new[] { "\r\n", "\n", "\r" }, System.StringSplitOptions.None);

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    paragraph.Inlines.Add(new Run("\n"));
                    continue;
                }

                FormatLine(paragraph, line);
                paragraph.Inlines.Add(new Run("\n"));
            }

            return paragraph;
        }

        /// <summary>
        /// Форматирует отдельную строку кода
        /// </summary>
        private static void FormatLine(Paragraph paragraph, string line)
        {
            // Проверяем препроцессорные директивы
            if (PreprocessorRegex.IsMatch(line))
            {
                paragraph.Inlines.Add(CreateRun(line, TokenType.Preprocessor));
                return;
            }

            // Проверяем однострочные комментарии
            if (SingleLineCommentRegex.IsMatch(line))
            {
                var match = SingleLineCommentRegex.Match(line);
                var codeBefore = line.Substring(0, match.Index);
                var comment = match.Value;

                if (!string.IsNullOrEmpty(codeBefore))
                {
                    FormatCodeSegment(paragraph, codeBefore);
                }
                paragraph.Inlines.Add(CreateRun(comment, TokenType.Comment));
                return;
            }

            // Форматируем всю строку
            FormatCodeSegment(paragraph, line);
        }

        /// <summary>
        /// Форматирует сегмент кода
        /// </summary>
        private static void FormatCodeSegment(Paragraph paragraph, string segment)
        {
            if (string.IsNullOrEmpty(segment))
                return;

            // Ищем строковые литералы
            var stringMatches = StringLiteralRegex.Matches(segment);
            if (stringMatches.Count > 0)
            {
                int lastIndex = 0;
                foreach (Match match in stringMatches)
                {
                    // Текст перед строковым литералом
                    if (match.Index > lastIndex)
                    {
                        FormatCodeWithoutStrings(paragraph, segment.Substring(lastIndex, match.Index - lastIndex));
                    }

                    // Строковый литерал
                    paragraph.Inlines.Add(CreateRun(match.Value, TokenType.StringLiteral));
                    lastIndex = match.Index + match.Length;
                }

                // Остаток после последнего строкового литерала
                if (lastIndex < segment.Length)
                {
                    FormatCodeWithoutStrings(paragraph, segment.Substring(lastIndex));
                }
            }
            else
            {
                FormatCodeWithoutStrings(paragraph, segment);
            }
        }

        /// <summary>
        /// Форматирует код без строковых литералов
        /// </summary>
        private static void FormatCodeWithoutStrings(Paragraph paragraph, string code)
        {
            if (string.IsNullOrEmpty(code))
                return;

            // Ищем числа
            var numberMatches = NumberRegex.Matches(code);
            if (numberMatches.Count > 0)
            {
                int lastIndex = 0;
                foreach (Match match in numberMatches)
                {
                    // Текст перед числом
                    if (match.Index > lastIndex)
                    {
                        FormatIdentifiersAndOperators(paragraph, code.Substring(lastIndex, match.Index - lastIndex));
                    }

                    // Число
                    paragraph.Inlines.Add(CreateRun(match.Value, TokenType.NumberLiteral));
                    lastIndex = match.Index + match.Length;
                }

                // Остаток после последнего числа
                if (lastIndex < code.Length)
                {
                    FormatIdentifiersAndOperators(paragraph, code.Substring(lastIndex));
                }
            }
            else
            {
                FormatIdentifiersAndOperators(paragraph, code);
            }
        }

        /// <summary>
        /// Форматирует идентификаторы и операторы
        /// </summary>
        private static void FormatIdentifiersAndOperators(Paragraph paragraph, string code)
        {
            if (string.IsNullOrEmpty(code))
                return;

            // Ищем идентификаторы
            var identifierMatches = IdentifierRegex.Matches(code);
            if (identifierMatches.Count > 0)
            {
                int lastIndex = 0;
                foreach (Match match in identifierMatches)
                {
                    // Операторы и пробелы перед идентификатором
                    if (match.Index > lastIndex)
                    {
                        paragraph.Inlines.Add(CreateRun(
                            code.Substring(lastIndex, match.Index - lastIndex),
                            TokenType.Operator));
                    }

                    // Идентификатор (ключевое слово, тип или обычный идентификатор)
                    var identifier = match.Value;
                    var tokenType = ClassifyIdentifier(identifier);
                    paragraph.Inlines.Add(CreateRun(identifier, tokenType));

                    lastIndex = match.Index + match.Length;
                }

                // Операторы после последнего идентификатора
                if (lastIndex < code.Length)
                {
                    paragraph.Inlines.Add(CreateRun(
                        code.Substring(lastIndex),
                        TokenType.Operator));
                }
            }
            else
            {
                // Только операторы
                paragraph.Inlines.Add(CreateRun(code, TokenType.Operator));
            }
        }

        /// <summary>
        /// Классифицирует идентификатор
        /// </summary>
        private static TokenType ClassifyIdentifier(string identifier)
        {
            if (Keywords.Contains(identifier))
                return TokenType.Keyword;

            if (BuiltInTypes.Contains(identifier))
                return TokenType.TypeName;

            return TokenType.Text;
        }

        /// <summary>
        /// Создает Run с соответствующим форматированием
        /// </summary>
        private static Run CreateRun(string text, TokenType type)
        {
            var run = new Run(text)
            {
                Foreground = GetBrushForTokenType(type)
            };

            if (type == TokenType.Keyword)
                run.FontWeight = System.Windows.FontWeights.Bold;
            else if (type == TokenType.TypeName)
                run.FontWeight = System.Windows.FontWeights.SemiBold;

            return run;
        }

        /// <summary>
        /// Создает кодовый блок с подсветкой синтаксиса
        /// </summary>
        public static Paragraph CreateCodeBlock(string code, bool withBackground = true)
        {
            var paragraph = FormatCode(code);

            if (withBackground)
            {
                paragraph.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 240, 240, 245));
                paragraph.Padding = new System.Windows.Thickness(12);
                paragraph.BorderBrush = Brushes.LightGray;
                paragraph.BorderThickness = new System.Windows.Thickness(1);
                paragraph.Margin = new System.Windows.Thickness(0, 10, 0, 10);
            }

            return paragraph;
        }

        /// <summary>
        /// Определяет цвет для типа токена
        /// </summary>
        private static Brush GetBrushForTokenType(TokenType type)
        {
            return type switch
            {
                TokenType.Keyword => KeywordBrush,
                TokenType.TypeName => TypeBrush,
                TokenType.StringLiteral => StringBrush,
                TokenType.NumberLiteral => NumberBrush,
                TokenType.Comment => CommentBrush,
                TokenType.Preprocessor => PreprocessorBrush,
                TokenType.Operator => OperatorBrush,
                _ => TextBrush
            };
        }

        /// <summary>
        /// Типы токенов для подсветки
        /// </summary>
        private enum TokenType
        {
            Text,
            Keyword,
            TypeName,
            StringLiteral,
            NumberLiteral,
            Comment,
            Preprocessor,
            Operator
        }
    }
}