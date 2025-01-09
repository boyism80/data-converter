using ExcelTableConverter.Model;
using System.Text;

namespace ExcelTableConverter
{
    public enum TextAlign
    {
        Left, Right, Center
    }

    public static class Logger
    {
        private static int _y = 1;
        private static int _width, _height;
        private static int _commentLine;

        private static readonly HashSet<string> _history = new HashSet<string>();
        private static readonly Mutex _errorFilesMutex = new Mutex();
        private static readonly HashSet<string> _errorFiles = new HashSet<string>();

        public static IReadOnlyList<string> ErrorFiles => _errorFiles.ToList();
        public static bool TTY { get; private set; }
        public static Func<string, string> OnDecorate;

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

        public static void Write(string text, ConsoleColor foreground = ConsoleColor.White, TextAlign align = TextAlign.Left, bool decorate = true)
        {
            if (OnDecorate != null && decorate)
                text = OnDecorate(text);

            var x = 0;
            switch (align)
            {
                case TextAlign.Left:
                    x = 0;
                    break;

                case TextAlign.Center:
                    x = (Console.WindowWidth - text.Length - 1) / 2;
                    break;

                case TextAlign.Right:
                    x = Console.WindowWidth - text.Length - 1;
                    break;
            }
            text = $"{new string(' ', x)}{text}";

            lock (Console.Out)
            {
                if (!TTY)
                {
                    Console.WriteLine(text);
                    return;
                }

                var beforeForeground = Console.ForegroundColor;
                Console.ForegroundColor = foreground;
                Position(_y - _commentLine);
                Clear();
                Console.Write(text);
                Console.ForegroundColor = beforeForeground;
            }
        }

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
            };
        }

        public static void Position(int y)
        {
            if (!TTY)
                return;

            _y = Math.Max(0, Math.Min(Console.WindowHeight - 1, y));
            Console.SetCursorPosition(0, _y);
        }

        public static int Position()
        {
            if (!TTY)
                return 0;

            return _y;
        }

        public static void NewLine()
        {
            if (!TTY)
                return;

            _y += (_commentLine + 1);
            _commentLine = 0;
            Console.WriteLine();
        }

        private static void Clear()
        {
            Console.Write(new string(' ', Console.WindowWidth));
            Position(_y);
        }

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

        public static void Complete(string text)
        {
            Write(text);
            _history.Clear();
            NewLine();
        }

        public static void Reset()
        {
            NewLine();
            _history.Clear();
        }
    }
}
