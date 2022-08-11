using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Quackers.TestLogger
{
    public class ConsoleLogger : ILogger
    {
        public string PassLabel { get; set; } = "✔";
        public string FailLabel { get; set; } = "❌";
        public string NoneLabel { get; set; } = "❓";
        public string SkipLabel { get; set; } = "🚫";
        public string NotFoundLabel { get; set; } = "🤷";
        public bool NoColor { get; set; } = false;
        public bool VerboseSummary { get; set; } = false;
        public bool OutputFailuresInline { get; set; } = false;
        public string SummaryStartMarker { get; set; }
        public string SummaryCompleteMarker { get; set; }
        public string FailureStartMarker { get; set; }
        public string LogPrefix { get; set; }
        public string TestNamePrefix { get; set; }
        public string FailureIndexPlaceholder { get; set; }

        private int _passed;
        private int _skipped;
        private int _failed;

        public void ShowSummary()
        {
            PrintIfNotNull(SummaryStartMarker);
            if (VerboseSummary)
            {
                ShowVerboseDetails();
            }

            if (_errors.Count == 0)
            {
                // nothing to report
                PrintIfNotNull(SummaryCompleteMarker);
                return;
            }

            InsertBreak();
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

            PrintIfNotNull(SummaryCompleteMarker);
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
        }

        private void LogStoredTestFailure(int idx, TestResultEventArgs e)
        {
            var idxPart = FailureIndexPlaceholder ?? $"[{idx}]";
            LogError($"{idxPart} {e.Result.TestCase.FullyQualifiedName}");
            foreach (var line in PrefixEachLine(e.Result.ErrorMessage, STORED_TEST_FAILURE_INDENT))
            {
                LogErrorMessage(line);
            }

            foreach (var line in PrefixEachLine(e.Result.ErrorStackTrace, STORED_TEST_FAILURE_INDENT))
            {
                LogStacktrace(line);
            }
        }
        
        private const string STORED_TEST_FAILURE_INDENT = "  ";
        private const string IMMEDIATE_TEST_FAILURE_INDENT = "    ";

        private static IEnumerable<string> PrefixEachLine(string str, string prefix)
        {
            str ??= "";
            var parts = str.Split(new[] { "\n" }, StringSplitOptions.None)
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

        public void LogError(string str)
        {
            Console.WriteLine($"{LogPrefix}{str.BrightRed()}");
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
            var duration = DurationStringFor(e.Result.Duration);
            var str = TestNameFor(e);
            Log($"{Prefix(PassLabel, str).BrightGreen()} [{duration}]");
        }

        private string TestNameFor(TestResultEventArgs e)
        {
            return $"{TestNamePrefix}{e.Result.TestCase.FullyQualifiedName}";
        }

        private string DurationStringFor(TimeSpan resultDuration)
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
                var dec = (decimal) ms / 1000M;
                return $"{dec:0.00} s";
            }

            return $"{resultDuration}";
        }

        public void LogFail(TestResultEventArgs e)
        {
            var duration = DurationStringFor(e.Result.Duration);
            Log($"{Prefix(FailLabel, TestNameFor(e)).BrightRed()} [{duration}]");
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

        private readonly List<TestResultEventArgs> _errors = new();
        private DateTime _started;

        public void LogNone(TestResultEventArgs e)
        {
            var name = TestNameFor(e);
            var reason = e.Result.ErrorMessage;
            Log($"{Prefix(NoneLabel, name).Grey()} [ {reason.DarkGrey()} ]");
        }

        public void LogSkipped(TestResultEventArgs e)
        {
            _skipped++;
            var str = TestNameFor(e);
            var reason = e.Result.ErrorMessage;
            Log($"{Prefix(SkipLabel, str).Grey()} [ {reason.DarkGrey()} ]");
        }

        public void LogNotFound(TestResultEventArgs e)
        {
            _skipped++;
            var str = TestNameFor(e);
            Log($"{Prefix(NotFoundLabel, str).BrightMagenta()}");
        }

        public void LogErrorMessage(string str)
        {
            Log(str.BrightMagenta());
        }

        public void LogStacktrace(string str)
        {
            Log(str.BrightCyan());
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