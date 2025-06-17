using ExcelTableConverter.Model;
using System.Text;

namespace ExcelTableConverter
{
    /// <summary>
    /// Text alignment options for console output
    /// </summary>
    public enum TextAlign
    {
        Left, Right, Center
    }

    /// <summary>
    /// Static logger class for console output with TTY support and formatting
    /// 
    /// Provides centralized logging functionality with support for both TTY
    /// and non-TTY environments, including color formatting, text alignment,
    /// and error tracking capabilities.
    /// </summary>
    public static class Logger
    {
        private static int _y = 1;
        private static int _width, _height;
        private static int _commentLine;

        private static readonly HashSet<string> _history = new HashSet<string>();
        private static readonly Mutex _errorFilesMutex = new Mutex();
        private static readonly HashSet<string> _errorFiles = new HashSet<string>();

        /// <summary>
        /// Gets the list of files that encountered errors during processing
        /// </summary>
        public static IReadOnlyList<string> ErrorFiles => _errorFiles.ToList();
        
        /// <summary>
        /// Gets a value indicating whether the application is running in TTY mode
        /// </summary>
        public static bool TTY { get; private set; }
        
        /// <summary>
        /// Gets or sets the text decoration function for output formatting
        /// </summary>
        public static Func<string, string> OnDecorate;

        /// <summary>
        /// Initializes the Logger static class
        /// 
        /// Sets up TTY detection, console cursor visibility, and UTF-8 encoding
        /// </summary>
        static Logger()
        {
#if DISABLED_TTY
            TTY = false;
#else
            TTY = Environment.UserInteractive;
#endif

            if (TTY)
            {
                Console.CursorVisible = false;
            }

            Console.OutputEncoding = Encoding.UTF8;
        }

        /// <summary>
        /// Writes text to the console with specified formatting options
        /// </summary>
        /// <param name="text">The text to write</param>
        /// <param name="foreground">The foreground color for the text</param>
        /// <param name="align">The text alignment option</param>
        /// <param name="decorate">Whether to apply text decoration</param>
        public static void Write(ReadOnlySpan<char> text, ConsoleColor foreground = ConsoleColor.White, TextAlign align = TextAlign.Left, bool decorate = true)
        {
            if (OnDecorate != null && decorate)
            {
                text = OnDecorate(text.ToString()).AsSpan(); // OnDecorate는 string 반환하므로 변환 필요
            }

            // Calculate horizontal position based on alignment
            var x = align switch
            {
                TextAlign.Left => 0,
                TextAlign.Center => (Console.WindowWidth - text.Length - 1) / 2,
                TextAlign.Right => Console.WindowWidth - text.Length - 1,
                _ => 0,
            };

            var sb = new StringBuilder(Console.WindowWidth);
            sb.Append(' ', x); // 정렬된 위치에 공백 추가
            sb.Append(text); // 본문 추가

            lock (Console.Out)
            {
                if (!TTY)
                {
                    Console.WriteLine(sb.ToString());
                    return;
                }

                var beforeForeground = Console.ForegroundColor;
                Console.ForegroundColor = foreground;
                Position(_y - _commentLine);
                Clear();
                Console.Write(sb.ToString());
                Console.ForegroundColor = beforeForeground;
            }
        }

        /// <summary>
        /// Writes text to the console with specified formatting options
        /// </summary>
        /// <param name="text">The text to write</param>
        /// <param name="foreground">The foreground color for the text</param>
        /// <param name="align">The text alignment option</param>
        /// <param name="decorate">Whether to apply text decoration</param>
        public static void Write(string text, ConsoleColor foreground = ConsoleColor.White, TextAlign align = TextAlign.Left, bool decorate = true)
        {
            Write(text.AsSpan(), foreground, align, decorate);
        }

        /// <summary>
        /// Writes a line of text to the console with specified formatting options
        /// </summary>
        /// <param name="text">The text to write</param>
        /// <param name="foreground">The foreground color for the text</param>
        /// <param name="align">The text alignment option</param>
        /// <param name="decorate">Whether to apply text decoration</param>
        public static void WriteLine(string text, ConsoleColor foreground = ConsoleColor.White, TextAlign align = TextAlign.Left, bool decorate = true)
        {
            if (!TTY)
            {
                Write(text, foreground, align, decorate);
            }
            else
            {
                Write(text, foreground, align, decorate);
                NewLine();
            }
            ;
        }

        /// <summary>
        /// Sets the cursor position to the specified line
        /// </summary>
        /// <param name="y">The line number to position the cursor at</param>
        public static void Position(int y)
        {
            if (!TTY)
                return;

            y = Math.Max(0, Math.Min(Console.WindowHeight - 1, y));
            Console.SetCursorPosition(0, y);
        }

        /// <summary>
        /// Gets the current cursor position
        /// </summary>
        /// <returns>The current line number</returns>
        public static int Position()
        {
            if (!TTY)
                return 0;

            return _y;
        }

        /// <summary>
        /// Advances to the next line and resets comment line counter
        /// </summary>
        public static void NewLine()
        {
            if (!TTY)
                return;

            _y += (_commentLine + 1);
            _commentLine = 0;
            Console.WriteLine();
        }

        /// <summary>
        /// Clears the current line in TTY mode
        /// </summary>
        private static void Clear()
        {
            Console.Write(new string(' ', Console.WindowWidth));
            Position(_y - _commentLine);
        }

        /// <summary>
        /// Writes a comment line with specified color
        /// </summary>
        /// <param name="text">The comment text to write</param>
        /// <param name="foreground">The foreground color for the comment</param>
        public static void Comment(string text, ConsoleColor foreground = ConsoleColor.White)
        {
            if (!TTY)
            {
                Console.WriteLine(text);
            }
            else
            {
                var beforeForeground = Console.ForegroundColor;
                Console.ForegroundColor = foreground;
                Console.WriteLine();
                Console.Write(text);
                _commentLine++;
                _y++;
                Console.ForegroundColor = beforeForeground;
            }

            lock (Console.Out)
            {

            }
        }

        /// <summary>
        /// Logs an error message and tracks the associated file
        /// </summary>
        /// <param name="text">The error message to log</param>
        /// <param name="tracker">Optional file tracker for error source identification</param>
        public static void Error(string text, IExcelFileTrackable tracker = null)
        {
            _errorFilesMutex.WaitOne();
            if (tracker != null)
            {
                _errorFiles.Add(tracker.FileName);
            }
            _errorFilesMutex.ReleaseMutex();

            var suffix = tracker != null ? $"=> {tracker.FileName}:{tracker.SheetName}" : string.Empty;
            lock (Console.Out)
            {
                Comment($"{text} {suffix}", foreground: ConsoleColor.Red);
            }
        }

        /// <summary>
        /// Logs a completion message and clears history
        /// </summary>
        /// <param name="text">The completion message to log</param>
        public static void Complete(string text)
        {
            Write(text);
            _history.Clear();
            NewLine();
        }

        /// <summary>
        /// Resets the logger state and clears history
        /// </summary>
        public static void Reset()
        {
            NewLine();
            _history.Clear();
        }
    }
}
