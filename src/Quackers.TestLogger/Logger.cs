using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Quackers.TestLogger
{
    [FriendlyName(FriendlyName)]
    [ExtensionUri(ExtensionUri)]
    public class Logger : ITestLoggerWithParameters
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public const string FriendlyName = "quackers";

        // ReSharper disable once MemberCanBePrivate.Global
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/QuackersLogger/v1";

        private ILogger _logger;

        public void Initialize(TestLoggerEvents events, string testRunDirectory)
        {
            SubscribeToEvents(events);
        }

        public void Initialize(TestLoggerEvents events, Dictionary<string, string> parameters)
        {
            try
            {
                _logger = new ConsoleLogger();
                EnableDebugMessagesIfRequired(parameters);
                SetLoggerPropsFromEnvironment();
                SetLoggerPropsFrom(parameters);
                if (_logger.NoColor)
                {
                    Debug("Disabling color output");
                    StringExtensions.DisableColor = true;
                }

                SubscribeToEvents(events);
            }
            catch (Exception ex)
            {
                DumpException(ex);
            }
        }

        private static void EnableDebugMessagesIfRequired(Dictionary<string, string> parameters)
        {
            if (parameters.TryGetValue("debug", out var result))
            {
                var shouldDebug = TruthyValues.Contains(result?.ToLower());
                if (shouldDebug)
                {
                    Debug = s => Console.WriteLine($"DEBUG: {s}".BrightBlue());
                }
            }
        }

        private static readonly HashSet<string> TruthyValues = new(new[] { "yes", "true", "1", "on" });

        private static void DumpException(Exception ex, [CallerMemberName] string caller = null)
        {
            Console.WriteLine($"Error running '{caller}': {ex.Message}\n{ex.StackTrace}");
        }

        private void SetLoggerPropsFromEnvironment()
        {
            var envVars = Environment.GetEnvironmentVariables();
            var keys = envVars.Keys;
            var foundInvalid = false;
            foreach (string key in keys)
            {
                if (!key.StartsWith(ENVIRONMENT_VARIABLE_PREFIX, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var sanitisedKey = key.Substring(ENVIRONMENT_VARIABLE_PREFIX.Length);
                if (!LoggerOptionPropMap.TryGetValue(sanitisedKey, out var prop))
                {
                    Warn(
                        $"Unrecognised quackers environment variable: {key} (looking for {sanitisedKey} in {string.Join(",", LoggerOptionPropMap.Keys)})");
                    foundInvalid = true;
                    continue;
                }

                Debug($"Quackers env var found: {key} -> {sanitisedKey}");

                var envValue = Environment.GetEnvironmentVariable(key);
                SetLoggerProp(prop, envValue);
            }

            if (foundInvalid)
            {
                Warn(
                    $"Valid quackers environment variables are:\n- {string.Join("\n- ", LoggerOptionPropMap.Keys.Select(k => $"QUACKERS_{k.ToUpper()}"))}");
            }
        }

        private void SetLoggerProp(PropertyInfo prop, string value)
        {
            if (value is null)
            {
                return;
            }

            if (prop.PropertyType == typeof(string))
            {
                Debug($"Setting {prop.Name} to {value}");
                prop.SetValue(_logger, value);
                return;
            }

            if (prop.PropertyType == typeof(bool))
            {
                prop.SetValue(_logger, TruthyValues.Contains(value));
                return;
            }

            Warn($"Unhandled logger property type: {prop.PropertyType} for '{prop.Name}' - prop is ignored");
        }

        private static void Warn(string str)
        {
            Console.Error.WriteLine($"WARNING: {str}".BrightRed());
        }

        // ReSharper disable once InconsistentNaming
        private static Action<string> Debug = _ =>
        {
        };

        private const string ENVIRONMENT_VARIABLE_PREFIX = "QUACKERS_";

        private void SetLoggerPropsFrom(Dictionary<string, string> parameters)
        {
            if (!parameters.Any())
            {
                return;
            }

            WarnForUnknownParameters(parameters);

            foreach (var prop in LoggerOptionProps)
            {
                if (!parameters.TryGetValue(prop.Name, out var providedValue))
                {
                    Debug($"Can't find prop {prop.Name} in '{string.Join(",", parameters.Keys)}'");
                    continue;
                }

                if (providedValue is null)
                {
                    continue;
                }

                Debug($"Set prop: {prop.Name} to {providedValue}");
                SetLoggerProp(prop, providedValue);
            }
        }

        private void WarnForUnknownParameters(Dictionary<string, string> parameters)
        {
            var foundInvalid = false;
            foreach (var kvp in parameters)
            {
                if (IgnoreParameters.Contains(kvp.Key))
                {
                    continue;
                }

                if (!LoggerOptionPropMap.ContainsKey(kvp.Key))
                {
                    foundInvalid = true;
                    Warn($"Unrecognised quackers parameter: {kvp.Key}={kvp.Value}");
                }
            }

            if (foundInvalid)
            {
                Warn($@"Valid quackers parameters are:
- {
    string.Join("\n- ", LoggerOptionPropMap.Keys.Select(k => $"{k.ToLower()} :: {LoggerOptionPropMap[k].PropertyType}"))
}
Parameters are case-insensitive. Boolean parameters can be set with values yes/no, 1/0, true/false");
            }
        }

        private static readonly HashSet<string> IgnoreParameters = new(
            new[] { "TestRunDirectory", "TargetFramework", "debug" },
            StringComparer.OrdinalIgnoreCase
        );

        private static readonly PropertyInfo[] LoggerOptionProps = typeof(ILoggerProperties)
            .GetProperties()
            .Union(typeof(ILoggerProperties).GetInterfaces()
                .SelectMany(t => t.GetProperties())
            ).ToArray();

        private static readonly Dictionary<string, PropertyInfo> LoggerOptionPropMap =
            LoggerOptionProps.ToDictionary(pi => pi.Name, pi => pi, StringComparer.OrdinalIgnoreCase);


        private void SubscribeToEvents(TestLoggerEvents events)
        {
            events.TestResult += OnTestResult;
            events.TestRunComplete += OnTestRunComplete;
            events.TestRunStart += OnTestRunStarted;
        }

        private void OnTestRunStarted(object sender, TestRunStartEventArgs e)
        {
            _logger.Reset();
        }


        private void OnTestRunComplete(object sender, TestRunCompleteEventArgs e)
        {
            _logger.ShowSummary();
        }

        private void OnTestResult(object sender, TestResultEventArgs e)
        {
            var testName = e.Result.TestCase.FullyQualifiedName;
            switch (e.Result.Outcome)
            {
                case TestOutcome.None:
                    _logger.LogNone(testName, e.Result.ErrorMessage);
                    break;
                case TestOutcome.Passed:
                    _logger.LogPass(testName, e.Result.Duration);
                    break;
                case TestOutcome.Failed:
                    _logger.LogFail(e);
                    break;
                case TestOutcome.Skipped:
                    _logger.LogSkipped(testName, e.Result.ErrorMessage);
                    break;
                case TestOutcome.NotFound:
                    _logger.LogNotFound(testName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public interface ILoggerProperties
    {
        string PassLabel { get; set; }
        string FailLabel { get; set; }
        string NoneLabel { get; set; }
        string SkipLabel { get; set; }
        string NotFoundLabel { get; set; }
        bool NoColor { get; set; }
        bool VerboseSummary { get; set; }
        string SummaryStartMarker { get; set; }
        string SummaryCompleteMarker { get; set; }
        string FailureStartMarker { get; set; }
        string LogPrefix { get; set; }
        bool OutputFailuresInline { get; set; }
    }

    public interface ILogger : ILoggerProperties
    {
        void LogInfo(string str);
        void LogError(string str);
        void LogPass(string str, TimeSpan resultDuration);
        void LogFail(TestResultEventArgs e);
        void LogNone(string name, string reason);
        void LogSkipped(string str, string reason);
        void LogNotFound(string str);
        void LogErrorMessage(string str);
        void LogStacktrace(string str);
        void InsertBreak();
        void ShowSummary();
        void Reset();
    }

    public class ConsoleLogger : ILogger
    {
        public string PassLabel { get; set; } = "✔️";
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
            LogError("Failures:");
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
            LogError($"[{idx}] {e.Result.TestCase.FullyQualifiedName}");
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

        public void LogPass(string str, TimeSpan resultDuration)
        {
            _passed++;
            var duration = DurationStringFor(resultDuration);
            Log($"{Prefix(PassLabel, str).BrightGreen()} [{duration}]");
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
            Log($"{Prefix(FailLabel, e.Result.TestCase.FullyQualifiedName).BrightRed()} [{duration}]");
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

        public void LogNone(string name, string reason)
        {
            Log($"{Prefix(NoneLabel, name).Grey()} [ {reason.DarkGrey()} ]");
        }

        public void LogSkipped(string str, string reason)
        {
            _skipped++;
            Log($"{Prefix(SkipLabel, str).Grey()} [ {reason.DarkGrey()} ]");
        }

        public void LogNotFound(string str)
        {
            _skipped++;
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