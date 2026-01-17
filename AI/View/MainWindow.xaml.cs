using AI.ViewModels;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using AI.Utils;

namespace AI.View
{
    public partial class MainWindow : Window
    {
        // Статические предкомпилированные регулярные выражения
        private static readonly Regex NumberedListItemRegex =
            new Regex(@"^\d+\.\s", RegexOptions.Compiled);

        private static readonly Regex NumberedListItemWithContentRegex =
            new Regex(@"^(\d+)\.\s+(.+)$", RegexOptions.Compiled);

        private static readonly Regex CodeBlockStartRegex =
            new Regex(@"^```(\w*)?$", RegexOptions.Compiled);

        private static readonly Regex HeaderRegex =
            new Regex(@"^###\s+(.+)$", RegexOptions.Compiled);

        private static readonly Regex SeparatorRegex =
            new Regex(@"^---\s*$", RegexOptions.Compiled);

        private static readonly Regex InlineCodeRegex =
            new Regex(@"`([^`]+)`", RegexOptions.Compiled);

        private static readonly Regex BoldTextRegex =
            new Regex(@"\*\*([^*]+)\*\*", RegexOptions.Compiled);

        // Константы для разделителей строк
        private static readonly string[] LineSeparators = { "\r\n", "\n", "\r" };
        private static readonly string[] BoldSeparators = { "**" };
        private static readonly char[] CodeSeparators = { '`' };

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            InputTextBox.Focus();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
                FormatResponse("");
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.OutputText))
            {
                var viewModel = (MainViewModel)sender;
                Dispatcher.Invoke(() =>
                {
                    FormatResponse(viewModel.OutputText);
                });
            }
        }

        public void FormatResponse(string text)
        {
            if (ResponseRichTextBox == null) return;

            // Используем существующий Document вместо создания нового
            FlowDocument flowDocument;
            if (ResponseRichTextBox.Document == null)
            {
                flowDocument = new FlowDocument();
                ResponseRichTextBox.Document = flowDocument;
            }
            else
            {
                flowDocument = ResponseRichTextBox.Document;
                flowDocument.Blocks.Clear(); // Очищаем содержимое
            }

            flowDocument.FontSize = 13;
            flowDocument.FontFamily = new FontFamily("Segoe UI");
            flowDocument.LineHeight = 1.3;

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            // Разделяем текст на строки
            var lines = text.Split(LineSeparators, StringSplitOptions.None);

            Paragraph currentParagraph = new Paragraph();
            currentParagraph.Margin = new Thickness(0, 3, 0, 3);

            bool inCodeBlock = false;
            StringBuilder codeBlockContent = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmedLine = line.Trim();

                // Обработка кодовых блоков
                if (CodeBlockStartRegex.IsMatch(trimmedLine))
                {
                    if (!inCodeBlock)
                    {
                        // Начало кодового блока
                        inCodeBlock = true;

                        // Сохраняем текущий параграф
                        if (currentParagraph.Inlines.Count > 0)
                        {
                            flowDocument.Blocks.Add(currentParagraph);
                        }

                        currentParagraph = new Paragraph();
                        currentParagraph.Margin = new Thickness(0, 3, 0, 3);
                        codeBlockContent.Clear();
                    }
                    else
                    {
                        // Конец кодового блока
                        inCodeBlock = false;

                        // Добавляем кодовый блок
                        if (codeBlockContent.Length > 0)
                        {
                            var codeText = codeBlockContent.ToString().Trim();
                            var codeParagraph = CSharpSyntaxHighlighter.CreateCodeBlock(codeText);
                            flowDocument.Blocks.Add(codeParagraph);
                        }

                        currentParagraph = new Paragraph();
                        currentParagraph.Margin = new Thickness(0, 3, 0, 3);
                    }
                    continue;
                }

                // Если внутри кодового блока
                if (inCodeBlock)
                {
                    codeBlockContent.AppendLine(line);
                    continue;
                }

                // Проверяем на нумерованный список (цифра с точкой)
                if (NumberedListItemRegex.IsMatch(trimmedLine))
                {
                    // Сохраняем текущий параграф
                    if (currentParagraph.Inlines.Count > 0)
                    {
                        flowDocument.Blocks.Add(currentParagraph);
                    }

                    // Обрабатываем нумерованный список
                    i = ProcessNumberedList(flowDocument, lines, i);

                    // Создаем новый параграф
                    currentParagraph = new Paragraph();
                    currentParagraph.Margin = new Thickness(0, 3, 0, 3);
                    continue;
                }

                // Проверяем на маркированный список (звездочки, тире)
                if (IsListItem(line, trimmedLine))
                {
                    // Сохраняем текущий параграф
                    if (currentParagraph.Inlines.Count > 0)
                    {
                        flowDocument.Blocks.Add(currentParagraph);
                    }

                    // Обрабатываем маркированный список
                    i = ProcessBulletList(flowDocument, lines, i);

                    // Создаем новый параграф
                    currentParagraph = new Paragraph();
                    currentParagraph.Margin = new Thickness(0, 3, 0, 3);
                    continue;
                }

                // Обработка заголовков
                var headerMatch = HeaderRegex.Match(trimmedLine);
                if (headerMatch.Success)
                {
                    if (currentParagraph.Inlines.Count > 0)
                    {
                        flowDocument.Blocks.Add(currentParagraph);
                    }

                    currentParagraph = new Paragraph();
                    currentParagraph.Margin = new Thickness(0, 10, 0, 8);

                    var headerText = headerMatch.Groups[1].Value;
                    var headerRun = new Run(headerText);
                    headerRun.FontSize = 14;
                    headerRun.FontWeight = FontWeights.Bold;
                    headerRun.Foreground = Brushes.DarkBlue;

                    currentParagraph.Inlines.Add(headerRun);
                    flowDocument.Blocks.Add(currentParagraph);

                    currentParagraph = new Paragraph();
                    currentParagraph.Margin = new Thickness(0, 3, 0, 3);
                    continue;
                }

                // Обработка разделителей (---)
                if (SeparatorRegex.IsMatch(trimmedLine))
                {
                    if (currentParagraph.Inlines.Count > 0)
                    {
                        flowDocument.Blocks.Add(currentParagraph);
                    }

                    // Добавляем горизонтальную линию
                    var separatorParagraph = new Paragraph();
                    separatorParagraph.Margin = new Thickness(0, 8, 0, 8);
                    separatorParagraph.BorderBrush = Brushes.LightGray;
                    separatorParagraph.BorderThickness = new Thickness(0, 1, 0, 0);
                    separatorParagraph.LineHeight = 1;
                    flowDocument.Blocks.Add(separatorParagraph);

                    currentParagraph = new Paragraph();
                    currentParagraph.Margin = new Thickness(0, 3, 0, 3);
                    continue;
                }

                // Обработка пустых строк
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (currentParagraph.Inlines.Count > 0)
                    {
                        flowDocument.Blocks.Add(currentParagraph);
                        currentParagraph = new Paragraph();
                        currentParagraph.Margin = new Thickness(0, 3, 0, 3);
                    }
                    continue;
                }

                // Обработка форматированного текста
                ProcessFormattedText(currentParagraph, line);

                // Добавляем пробел между строками (если следующая строка не пустая)
                if (i < lines.Length - 1 && !string.IsNullOrWhiteSpace(lines[i + 1]))
                {
                    currentParagraph.Inlines.Add(new Run(" "));
                }
            }

            // Добавляем последний параграф
            if (currentParagraph.Inlines.Count > 0)
            {
                flowDocument.Blocks.Add(currentParagraph);
            }

            // Автопрокрутка к концу
            ResponseRichTextBox.ScrollToEnd();
        }

        // Новый метод для обработки форматированного текста
        private void ProcessFormattedText(Paragraph paragraph, string line)
        {
            if (string.IsNullOrEmpty(line))
                return;

            // Обработка inline кода (`code`)
            if (line.Contains("`"))
            {
                // Используем Regex для более точного разбора inline кода
                int lastIndex = 0;
                var matches = InlineCodeRegex.Matches(line);

                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        // Добавляем текст перед кодом
                        if (match.Index > lastIndex)
                        {
                            var textBefore = line.Substring(lastIndex, match.Index - lastIndex);
                            if (textBefore.Contains("**"))
                            {
                                ProcessBoldText(paragraph, textBefore);
                            }
                            else
                            {
                                paragraph.Inlines.Add(new Run(textBefore));
                            }
                        }

                        // Добавляем inline код
                        var codeRun = new Run(match.Groups[1].Value);
                        codeRun.Background = Brushes.LightYellow;
                        codeRun.FontFamily = new FontFamily("Consolas");
                        codeRun.Foreground = Brushes.DarkRed;
                        codeRun.FontSize = 11;
                        paragraph.Inlines.Add(codeRun);

                        lastIndex = match.Index + match.Length;
                    }

                    // Добавляем оставшийся текст
                    if (lastIndex < line.Length)
                    {
                        var remainingText = line.Substring(lastIndex);
                        if (remainingText.Contains("**"))
                        {
                            ProcessBoldText(paragraph, remainingText);
                        }
                        else
                        {
                            paragraph.Inlines.Add(new Run(remainingText));
                        }
                    }
                }
                else
                {
                    // Если нет совпадений с regex, используем старый метод
                    ProcessInlineCode(paragraph, line);
                }
            }
            // Обработка жирного текста (**текст**)
            else if (line.Contains("**"))
            {
                ProcessBoldText(paragraph, line);
            }
            else
            {
                // Обычный текст
                paragraph.Inlines.Add(new Run(line));
            }
        }

        // Метод для обработки жирного текста
        private void ProcessBoldText(Paragraph paragraph, string text)
        {
            var matches = BoldTextRegex.Matches(text);
            int lastIndex = 0;

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    // Добавляем текст перед жирным
                    if (match.Index > lastIndex)
                    {
                        var textBefore = text.Substring(lastIndex, match.Index - lastIndex);
                        if (textBefore.Contains("`"))
                        {
                            ProcessInlineCode(paragraph, textBefore);
                        }
                        else
                        {
                            paragraph.Inlines.Add(new Run(textBefore));
                        }
                    }

                    // Добавляем жирный текст
                    var boldRun = new Run(match.Groups[1].Value)
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.DarkSlateGray
                    };
                    paragraph.Inlines.Add(boldRun);

                    lastIndex = match.Index + match.Length;
                }

                // Добавляем оставшийся текст
                if (lastIndex < text.Length)
                {
                    var remainingText = text.Substring(lastIndex);
                    if (remainingText.Contains("`"))
                    {
                        ProcessInlineCode(paragraph, remainingText);
                    }
                    else
                    {
                        paragraph.Inlines.Add(new Run(remainingText));
                    }
                }
            }
            else
            {
                // Если нет совпадений с regex, используем старый метод
                var textParts = text.Split(BoldSeparators, StringSplitOptions.None);
                for (int j = 0; j < textParts.Length; j++)
                {
                    if (j % 2 == 0)
                    {
                        // Обычный текст (может содержать inline код)
                        if (textParts[j].Contains("`"))
                        {
                            ProcessInlineCode(paragraph, textParts[j]);
                        }
                        else
                        {
                            paragraph.Inlines.Add(new Run(textParts[j]));
                        }
                    }
                    else
                    {
                        // Жирный текст
                        var boldRun = new Run(textParts[j])
                        {
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.DarkSlateGray
                        };
                        paragraph.Inlines.Add(boldRun);
                    }
                }
            }
        }

        // Метод для обработки нумерованного списка
        private int ProcessNumberedList(FlowDocument flowDocument, string[] lines, int startIndex)
        {
            int i = startIndex;

            while (i < lines.Length)
            {
                var line = lines[i];
                var trimmedLine = line.Trim();

                // Проверяем, является ли строка нумерованным элементом списка
                var match = NumberedListItemWithContentRegex.Match(trimmedLine);

                if (match.Success)
                {
                    // Извлекаем номер и текст
                    int currentNumber = int.Parse(match.Groups[1].Value);
                    string itemText = match.Groups[2].Value;

                    // Создаем параграф для элемента списка
                    var listItemParagraph = new Paragraph();
                    listItemParagraph.Margin = new Thickness(20, 2, 0, 2);

                    // Добавляем номер
                    var numberRun = new Run($"{currentNumber}. ");
                    numberRun.Foreground = Brushes.DarkSlateGray;
                    numberRun.FontWeight = FontWeights.Bold;
                    listItemParagraph.Inlines.Add(numberRun);

                    // Обрабатываем содержимое элемента (inline код, жирный текст)
                    ProcessTextWithFormatting(listItemParagraph, itemText);

                    flowDocument.Blocks.Add(listItemParagraph);
                    i++;

                    // Проверяем многострочные элементы (продолжение без номера)
                    while (i < lines.Length &&
                           !string.IsNullOrWhiteSpace(lines[i]) &&
                           !NumberedListItemRegex.IsMatch(lines[i].Trim()) &&
                           !IsListItem(lines[i], lines[i].Trim()))
                    {
                        var continuationParagraph = new Paragraph();
                        continuationParagraph.Margin = new Thickness(35, 1, 0, 1);
                        ProcessTextWithFormatting(continuationParagraph, lines[i].Trim());
                        flowDocument.Blocks.Add(continuationParagraph);
                        i++;
                    }
                }
                else if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    // Пустая строка может разделять элементы списка
                    if (i + 1 < lines.Length &&
                        NumberedListItemRegex.IsMatch(lines[i + 1].Trim()))
                    {
                        i++;
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    // Если это не нумерованный элемент и не пустая строка, выходим
                    break;
                }
            }

            return i - 1;
        }

        // Метод для обработки маркированного списка
        private int ProcessBulletList(FlowDocument flowDocument, string[] lines, int startIndex)
        {
            int i = startIndex;

            while (i < lines.Length)
            {
                var line = lines[i];
                var trimmedLine = line.Trim();

                if (!IsListItem(line, trimmedLine))
                {
                    if (string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        // Проверяем следующую строку
                        if (i + 1 < lines.Length && !IsListItem(lines[i + 1], lines[i + 1].Trim()))
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                // Извлекаем текст элемента
                string itemText = GetListItemText(line, trimmedLine);

                // Создаем элемент списка
                var listItemParagraph = new Paragraph();
                listItemParagraph.Margin = new Thickness(20, 2, 0, 2);

                // Добавляем маркер
                var markerRun = new Run("• ");
                markerRun.Foreground = Brushes.DarkSlateGray;
                listItemParagraph.Inlines.Add(markerRun);

                // Обрабатываем содержимое элемента
                ProcessTextWithFormatting(listItemParagraph, itemText);

                flowDocument.Blocks.Add(listItemParagraph);
                i++;

                // Проверяем многострочные элементы
                while (i < lines.Length &&
                       !string.IsNullOrWhiteSpace(lines[i]) &&
                       !IsListItem(lines[i], lines[i].Trim()))
                {
                    var continuationParagraph = new Paragraph();
                    continuationParagraph.Margin = new Thickness(35, 1, 0, 1);
                    ProcessTextWithFormatting(continuationParagraph, lines[i].Trim());
                    flowDocument.Blocks.Add(continuationParagraph);
                    i++;
                }
            }

            return i - 1;
        }

        // Метод для извлечения текста из элемента списка
        private string GetListItemText(string line, string trimmedLine)
        {
            if (trimmedLine.StartsWith("* ") || trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("+ "))
            {
                return trimmedLine.Substring(2).Trim();
            }
            else if (line.TrimStart().StartsWith("* "))
            {
                int markerIndex = line.IndexOf("* ");
                return line.Substring(markerIndex + 2).TrimStart();
            }

            return trimmedLine;
        }

        // Метод для проверки элемента списка
        private bool IsListItem(string line, string trimmedLine)
        {
            return trimmedLine.StartsWith("* ") ||
                   trimmedLine.StartsWith("- ") ||
                   trimmedLine.StartsWith("+ ") ||
                   (line.TrimStart().StartsWith("* ") && !line.Contains("**"));
        }

        // Метод для обработки текста с форматированием (inline код, жирный текст)
        private void ProcessTextWithFormatting(Paragraph paragraph, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            ProcessFormattedText(paragraph, text);
        }

        // Метод для обработки inline кода (старый метод для обратной совместимости)
        private void ProcessInlineCode(Paragraph paragraph, string text)
        {
            var codeParts = text.Split(CodeSeparators);

            for (int k = 0; k < codeParts.Length; k++)
            {
                if (k % 2 == 0)
                {
                    // Обычный текст
                    paragraph.Inlines.Add(new Run(codeParts[k]));
                }
                else
                {
                    // Inline код
                    var codeRun = new Run(codeParts[k]);
                    codeRun.Background = Brushes.LightYellow;
                    codeRun.FontFamily = new FontFamily("Consolas");
                    codeRun.Foreground = Brushes.DarkRed;
                    codeRun.FontSize = 11;
                    paragraph.Inlines.Add(codeRun);
                }
            }
        }

        private async void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    // Shift+Enter - вставка новой строки
                    return;
                }
                else
                {
                    // Enter без Shift - отправка запроса
                    e.Handled = true;

                    var viewModel = DataContext as MainViewModel;
                    if (viewModel != null && viewModel.SendCommand.CanExecute(null))
                    {
                        await Task.Run(() => viewModel.SendCommand.Execute(null));
                        InputTextBox.Focus();
                        InputTextBox.Clear();
                    }
                }
            }
        }

        private void CopyAsText_Click(object sender, RoutedEventArgs e)
        {
            var textRange = new TextRange(
                ResponseRichTextBox.Document.ContentStart,
                ResponseRichTextBox.Document.ContentEnd);

            Clipboard.SetText(textRange.Text);
        }
    }

}