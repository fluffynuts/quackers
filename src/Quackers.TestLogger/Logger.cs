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
            switch (e.Result.Outcome)
            {
                case TestOutcome.None:
                    _logger.LogNone(e);
                    break;
                case TestOutcome.Passed:
                    _logger.LogPass(e);
                    break;
                case TestOutcome.Failed:
                    _logger.LogFail(e);
                    break;
                case TestOutcome.Skipped:
                    _logger.LogSkipped(e);
                    break;
                case TestOutcome.NotFound:
                    _logger.LogNotFound(e);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}