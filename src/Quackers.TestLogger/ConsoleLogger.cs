using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Quackers.TestLogger
{
    public class ConsoleLogger : ILogger
    {
        public string PassLabel { get; set; } = "✅";
        public string FailLabel { get; set; } = "🛑";
        public string NoneLabel { get; set; } = "❓";
        public string SkipLabel { get; set; } = "🚫";
        public string NotFoundLabel { get; set; } = "🤷";
        public bool HighlightSlowTests { get; set; } = true;
        public int SlowTestThresholdMs { get; set; } = 1000;
        public string DebugLogFile { get; set; }

        public bool NoColor { get; set; }
            = Environment.GetEnvironmentVariable("NO_COLOR") is not null;

        public string Theme
        {
            get => _theme;
            set => SetTheme(value);
        }

        private void SetTheme(string value)
        {
            _theme = value;
            StringColorExtensions.ThemeName = value;
        }

        private string _theme = "default";
        public bool VerboseSummary { get; set; } = false;
        public bool OutputFailuresInline { get; set; } = false;
        public bool ShowHelp { get; set; } = true;

        public string LogPrefix { get; set; }
        public string SummaryStartMarker { get; set; }
        public string SummaryCompleteMarker { get; set; }
        public string FailureStartMarker { get; set; }
        public string SlowSummaryStartMarker { get; set; }
        public string SlowSummaryCompleteMarker { get; set; }
        public string TestNamePrefix { get; set; }
        public string FailureIndexPlaceholder { get; set; }
        public string SlowIndexPlaceholder { get; set; }

        private int _passed;
        private int _skipped;
        private int _failed;

        private readonly object _debugFileLock = new();

        private void DebugLog(string str)
        {
            if (!CanLogToDebugFile)
            {
                return;
            }

            try
            {
                lock (_debugFileLock)
                {
                    File.AppendAllText(
                        DebugLogFile,
                        $"{str}\n"
                    );
                }
            }
            catch
            {
                // disable debug logging if it falls over
                _canLogToDebugFile = false;
            }
        }

        private bool CanLogToDebugFile
            => _canLogToDebugFile ??= DetermineIfCanLogToDebugFile();

        private bool DetermineIfCanLogToDebugFile()
        {
            if (string.IsNullOrWhiteSpace(DebugLogFile))
            {
                return false;
            }

            try
            {
                var container = Path.GetDirectoryName(DebugLogFile);
                if (container is not null && !Directory.Exists(container))
                {
                    Directory.CreateDirectory(container);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool? _canLogToDebugFile;

        public void ShowSummary()
        {
            PrintIfNotNull(SummaryStartMarker);
            PrintSlowTests();

            if (_errors.Count == 0)
            {
                // nothing to report
                PrintIfNotNull(SummaryCompleteMarker);
                return;
            }

            InsertBreak();
            PrintFailures();
            if (VerboseSummary)
            {
                InsertBreak();
                ShowVerboseDetails();
            }

            PrintIfNotNull(SummaryCompleteMarker);
        }

        private void PrintSlowTests()
        {
            PrintIfNotNull(SlowSummaryStartMarker);
            if (SlowSummaryStartMarker is null)
            {
                LogWarning("Slow tests:");
            }

            for (var i = 0; i < _slowTests.Count; i++)
            {
                LogSlowTest(i + 1, _slowTests[i]);
            }

            PrintIfNotNull(SlowSummaryCompleteMarker);
        }

        private void PrintFailures()
        {
            PrintIfNotNull(FailureStartMarker);
            if (FailureStartMarker is null)
            {
                LogError("Failures:");
            }

            for (var i = 0; i < _errors.Count; i++)
            {
                InsertBreak();
                LogStoredTestFailure(i + 1, _errors[i]);
            }
        }

        void PrintIfNotNull(string str)
        {
            if (str is not null)
            {
                LogInfo(str);
            }
        }

        private void ShowVerboseDetails()
        {
            var runTime = DateTime.Now - _started;
            LogInfo("\nTest results:");
            LogInfo($"  Passed:   {_passed}");
            LogInfo($"  Failed:   {_failed}");
            LogInfo($"  Skipped:  {_skipped}");
            LogInfo($"  Total:    {_passed + _failed + _skipped}");
            LogInfo($"  Run time: {runTime.TotalSeconds:0.00} seconds");
        }

        public void Reset()
        {
            _errors.Clear();
            _passed = 0;
            _skipped = 0;
            _failed = 0;
            _started = DateTime.Now;
            
            DebugDumpProps(this);
        }

        private void DebugDumpProps(object obj)
        {
            if (!CanLogToDebugFile)
            {
                return;
            }

            var lines = new List<string>();
            if (obj is not null)
            {
                var type = obj.GetType();
                var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                lines.Add($"Dumping {props.Length} properties on object of type {type}");
                foreach (var prop in props)
                {
                    lines.Add($"  {prop.Name} = {prop.GetValue(obj)}");
                }
            }

            DebugLog(string.Join("\n", lines));
        }

        private void LogStoredTestFailure(int idx, TestResultEventArgs e)
        {
            var idxPart = FailureIndexPlaceholder ?? $"[{idx}]";
            LogError($"{idxPart} {TestNameFor(e)}");
            foreach (var line in PrefixEachLine(e.Result.ErrorMessage, STORED_TEST_FAILURE_INDENT))
            {
                LogErrorMessage(line);
            }

            foreach (var line in PrefixEachLine(e.Result.ErrorStackTrace, STORED_TEST_FAILURE_INDENT))
            {
                LogStacktrace(line);
            }
        }

        private void LogSlowTest(
            int idx,
            TestResultEventArgs e
        )
        {
            var idxPart = SlowIndexPlaceholder ?? $"[{idx}]";
            LogWarning($"{idxPart} {TestNameFor(e)} ({DurationStringFor(e.Result.Duration, false)})");
        }

        private const string STORED_TEST_FAILURE_INDENT = "  ";
        private const string IMMEDIATE_TEST_FAILURE_INDENT = "    ";

        private static IEnumerable<string> PrefixEachLine(string str, string prefix)
        {
            str ??= "";
            var parts = str.Split(
                    new[]
                    {
                        "\n"
                    },
                    StringSplitOptions.None
                )
                .Select(s => s.Trim('\r'));
            foreach (var part in parts)
            {
                yield return $"{prefix}{part}";
            }
        }

        public void LogInfo(string str)
        {
            Log(str);
        }

        public void LogWarning(string str)
        {
            Console.WriteLine($"{LogPrefix}{str.Warn()}");
        }

        public void LogError(string str)
        {
            Console.WriteLine($"{LogPrefix}{str.Fail()}");
        }

        private void LogInlineTestFailure(TestResultEventArgs e)
        {
            foreach (var line in PrefixEachLine(e.Result.ErrorMessage, IMMEDIATE_TEST_FAILURE_INDENT))
            {
                LogErrorMessage(line);
            }

            foreach (var line in PrefixEachLine(e.Result.ErrorStackTrace, IMMEDIATE_TEST_FAILURE_INDENT))
            {
                LogStacktrace(line);
            }
        }

        public void LogPass(TestResultEventArgs e)
        {
            _passed++;
            var isSlow = IsSlow(e);
            var duration = DurationStringFor(e.Result.Duration, isSlow);

            var str = TestNameFor(e);
            Log($"{Prefix(PassLabel, str).Pass()} [{duration}]");
        }

        private string TestNameFor(TestResultEventArgs e)
        {
            return $"{TestNamePrefix}{e.Result.TestCase.FullyQualifiedName}";
        }

        private bool IsSlow(TestResultEventArgs e)
        {
            var result = HighlightSlowTests &&
                e.Result.Duration.TotalMilliseconds >= SlowTestThresholdMs;
            if (result)
            {
                StoreSlow(e);
            }

            return result;
        }

        private string DurationStringFor(
            TimeSpan resultDuration,
            bool markAsSlow
        )
        {
            return HighlightIfSlow(
                () =>
                {
                    var ms = resultDuration.TotalMilliseconds;
                    if (ms < 1)
                    {
                        return "< 1 ms";
                    }

                    if (ms < 1000)
                    {
                        return $"{ms} ms";
                    }

                    if (ms < 60000)
                    {
                        var dec = (decimal)ms / 1000M;
                        return $"{dec:0.00} s";
                    }

                    return $"{resultDuration}";
                },
                markAsSlow
            );
        }

        private string HighlightIfSlow(
            Func<string> func,
            bool isSlow
        )
        {
            return isSlow
                ? $"{func()} (slow)".HighlightSlow()
                : func();
        }

        public void LogFail(TestResultEventArgs e)
        {
            var isSlow = IsSlow(e);
            var duration = DurationStringFor(e.Result.Duration, isSlow);
            Log($"{Prefix(FailLabel, TestNameFor(e)).Fail()} [{duration}]");
            if (OutputFailuresInline)
            {
                LogInlineTestFailure(e);
            }

            StoreFailure(e);
        }

        private void StoreFailure(TestResultEventArgs e)
        {
            _failed++;
            _errors.Add(e);
        }

        private void StoreSlow(TestResultEventArgs e)
        {
            _slowTests.Add(e);
        }

        private readonly List<TestResultEventArgs> _errors = new();
        private readonly List<TestResultEventArgs> _slowTests = new();
        private DateTime _started;

        public void LogNone(TestResultEventArgs e)
        {
            var name = TestNameFor(e);
            var reason = e.Result.ErrorMessage;
            Log($"{Prefix(NoneLabel, name).Disabled()} [ {reason.DisabledReason()} ]");
        }

        public void LogSkipped(TestResultEventArgs e)
        {
            _skipped++;
            var str = TestNameFor(e);
            var reason = e.Result.ErrorMessage;
            Log($"{Prefix(SkipLabel, str).Disabled()} [ {reason.DisabledReason()} ]");
        }

        public void LogNotFound(TestResultEventArgs e)
        {
            _skipped++;
            var str = TestNameFor(e);
            Log($"{Prefix(NotFoundLabel, str).Error()}");
        }

        public void LogErrorMessage(string str)
        {
            Log(str.Error());
        }

        public void LogStacktrace(string str)
        {
            Log(str.StackTrace());
        }

        public void InsertBreak()
        {
            Console.WriteLine($"{LogPrefix}");
        }

        private void Log(string str)
        {
            var parts = str.Split('\n')
                .Select(s => s.Trim());
            foreach (var part in parts)
            {
                Console.WriteLine($"{LogPrefix}{part}");
            }
        }

        private string Prefix(string prefix, string str)
        {
            return $"{prefix} {str}";
        }
    }
}