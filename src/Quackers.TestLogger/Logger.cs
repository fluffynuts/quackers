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
        private readonly List<TestResultEventArgs> _errors = new();

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

        private static Action<string> Debug = s =>
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
            Reset();
        }

        private void Reset()
        {
            _errors.Clear();
        }

        private void OnTestRunComplete(object sender, TestRunCompleteEventArgs e)
        {
            WriteSummary();
        }

        private void WriteSummary()
        {
            _logger.InsertBreak();
            _logger.LogError("Failures:");
            for (var i = 0; i < _errors.Count; i++)
            {
                _logger.InsertBreak();
                LogStoredTestFailure(i + 1, _errors[i]);
            }
        }

        private void OnTestResult(object sender, TestResultEventArgs e)
        {
            var testName = e.Result.TestCase.FullyQualifiedName;
            switch (e.Result.Outcome)
            {
                case TestOutcome.None:
                    _logger.LogNone(testName);
                    break;
                case TestOutcome.Passed:
                    _logger.LogPass(testName, e.Result.Duration);
                    break;
                case TestOutcome.Failed:
                    LogImmediateTestFailure(e);
                    StoreError(e);
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

        private void LogImmediateTestFailure(TestResultEventArgs e, int? idx = null)
        {
            _logger.LogFail($"{e.Result.TestCase.FullyQualifiedName}");
        }

        private void LogStoredTestFailure(int idx, TestResultEventArgs e)
        {
            _logger.LogError($"[{idx}] {e.Result.TestCase.FullyQualifiedName}");
            _logger.LogErrorMessage($"  {e.Result.ErrorMessage}");
            foreach (var line in PrefixEachLine(e.Result.ErrorStackTrace, "  "))
            {
                _logger.LogStacktrace(line);
            }
        }

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

        private void StoreError(TestResultEventArgs testResult)
        {
            _errors.Add(testResult);
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
    }

    public interface ILogger : ILoggerProperties
    {
        void LogInfo(string str);
        void LogError(string str);
        void LogPass(string str, TimeSpan resultDuration);
        void LogFail(string str);
        void LogNone(string str);
        void LogSkipped(string str, string reason);
        void LogNotFound(string str);
        void LogErrorMessage(string str);
        void LogStacktrace(string str);
        void InsertBreak();
    }

    public class ConsoleLogger : ILogger
    {
        public string PassLabel { get; set; } = "✔️";
        public string FailLabel { get; set; } = "❌";
        public string NoneLabel { get; set; } = "❓";
        public string SkipLabel { get; set; } = "🚫";
        public string NotFoundLabel { get; set; } = "🤷";
        public bool NoColor { get; set; } = false;

        public void LogInfo(string str)
        {
            Log(str);
        }

        public void LogError(string str)
        {
            Console.Error.WriteLine(str.BrightRed());
        }

        public void LogPass(string str, TimeSpan resultDuration)
        {
            var duration = DurationStringFor(resultDuration);
            Log($"{Prefix(PassLabel, str).BrightGreen()} [{DurationStringFor(resultDuration)}]");
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

        public void LogFail(string str)
        {
            Log(Prefix(FailLabel, str).BrightRed());
        }

        public void LogNone(string str)
        {
            Log(Prefix(NoneLabel, str).Grey());
        }

        public void LogSkipped(string str, string reason)
        {
            Log($"{Prefix(SkipLabel, str).Grey()} [ {reason.DarkGrey()} ]");
        }

        public void LogNotFound(string str)
        {
            Log(Prefix(NotFoundLabel, str).BrightMagenta());
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
            Console.WriteLine("");
        }

        private void Log(string str)
        {
            Console.WriteLine(str);
        }

        private string Prefix(string prefix, string str)
        {
            return $"{prefix} {str}";
        }
    }
}