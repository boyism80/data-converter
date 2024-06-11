using ExcelTableConverter.Model;
using System.Diagnostics;
using System.Text;

namespace ExcelTableConverter
{
    public static class Logger
    {
        private static int _row = 0;
        private static int _item = 0;
        private static int _completeCount = 0;
        private static Stopwatch _timer = new Stopwatch();
        private static readonly HashSet<string> _history = new HashSet<string>();
        private static readonly Mutex _errorFilesMutex = new Mutex();
        private static readonly HashSet<string> _errorFiles = new HashSet<string>();

        public static int Job { get; set; }
        public static IReadOnlyList<string> ErrorFiles => _errorFiles.ToList();

        static Logger()
        {
#if !JENKINS
            Console.CursorVisible = false;
#endif
            Console.OutputEncoding = Encoding.UTF8;
            _timer.Start();
        }

        public static void Write(string text, bool withElapsedTime = true, bool withStep = true, int? percent = null, ConsoleColor foreground = ConsoleColor.White)
        {
            var prefix = string.Empty;
            if (withElapsedTime)
                prefix = _timer.Elapsed.ToString("mm\\:ss");

            if (withStep)
                prefix = $"{prefix} | {_completeCount + 1,3}/{Job}";

            if (percent != null)
                prefix = $"{prefix} | {percent,3}%";

            if (string.IsNullOrEmpty(prefix) == false)
                text = $"[{prefix}] {text}";

            lock (Console.Out)
            {
#if !JENKINS
                Console.SetCursorPosition(0, _row);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, _row);
                if (foreground != ConsoleColor.White)
                    Console.ForegroundColor = foreground;
#endif

#if !JENKINS
                Console.Write(text);
#else
                Console.WriteLine(text);
#endif

#if !JENKINS
                if (foreground != ConsoleColor.White)
                    Console.ForegroundColor = ConsoleColor.White;
#endif
            }
        }

        public static void Append(string text, ConsoleColor foreground = ConsoleColor.White)
        {
            lock (Console.Out)
            {
                if (_history.Contains(text))
                    return;

                _item++;
#if !JENKINS
                Console.SetCursorPosition(0, _row + _item);
                if (foreground != ConsoleColor.White)
                    Console.ForegroundColor = foreground;
#endif

#if !JENKINS
                Console.Write($" - {text}");
#else
                Console.WriteLine($" - {text}");
#endif
#if !JENKINS
                if (foreground != ConsoleColor.White)
                    Console.ForegroundColor = ConsoleColor.White;
#endif

                _history.Add(text);
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
                Append($"{text} {suffix}", foreground: ConsoleColor.Red);
            }
        }

        public static void Next(int line = 1)
        {
            _row += _item + line;
            _item = 0;
        }

        public static void Complete(string text, bool withElapsedTime = true, bool withStep = true)
        {
            Write(text, withElapsedTime, withStep, 100);
            _completeCount++;
            _history.Clear();
            Next();
        }

        public static void Reset()
        {
            Next();

            _completeCount = 0;
            _item = 0;
            Job = 0;
            _history.Clear();
        }
    }
}
