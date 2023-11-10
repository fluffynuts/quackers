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
            if (!Console.OutputEncoding.Equals(System.Text.Encoding.UTF8))
            {
                Debug("Switching console output encoding to UTF8");
                // required for all unicode characters to display correctly
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            }

            try
            {
                _logger = new ConsoleLogger();

                EnableDebugMessagesIfRequired(parameters);
                SetLoggerPropsFromEnvironment();
                SetLoggerPropsFrom(parameters);
                _logger.DumpConfigIfRequired();

                if (_logger.NoColor)
                {
                    Debug("Disabling color output");
                    StringColorExtensions.DisableColor = true;
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
                    Debug = s => Console.WriteLine($"DEBUG: {s}".Debug());
                }
            }
        }

        private static readonly HashSet<string> TruthyValues = new(
            new[]
            {
                "yes",
                "true",
                "1",
                "on",
                "enable"
            },
            StringComparer.OrdinalIgnoreCase
        );

        private static readonly HashSet<string> FalsyValues = new(
            new[]
            {
                "no",
                "false",
                "0",
                "off",
                "disable"
            },
            StringComparer.OrdinalIgnoreCase
        );

        private static void DumpException(Exception ex, [CallerMemberName] string caller = null)
        {
            Console.Error.WriteLine(
                $"=================== LE ERROR ======================\nError running '{caller}': {ex.Message}\n{ex.StackTrace}\n==================== continue ================"
            );
        }

        private void SetLoggerPropsFromEnvironment()
        {
            var envVars = Environment.GetEnvironmentVariables();
            var keys = envVars.Keys;
            var foundInvalid = false;
            var helpParameterProvided = false;
            foreach (string key in keys)
            {
                if (!key.StartsWith(ENVIRONMENT_VARIABLE_PREFIX, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var prop = FindOptionProperty(key);
                if (prop is null)
                {
                    foundInvalid = true;
                    continue;
                }

                var envValue = Environment.GetEnvironmentVariable(key);
                SetLoggerProp(prop, envValue);
                if (prop.Name == nameof(ILoggerProperties.ShowHelp))
                {
                    helpParameterProvided = true;
                }
            }

            // show help if explicitly asked for or if not provided (defaults true) and an error was found
            var shouldShowHelp =
                (foundInvalid && _logger.ShowHelp) ||
                (helpParameterProvided && _logger.ShowHelp);
            if (shouldShowHelp)
            {
                Warn(
                    $@"Quackers configuration help:
- {string.Join("\n- ", LoggerOptionPropMap.Keys.Select(k => $"QUACKERS_{k.ToUpper()} : {HelpFor(k)}"))}

Flags can be set on with one of:  {string.Join(",", TruthyValues)}
Flags can be set off with one of: {string.Join(",", FalsyValues)}
"
                );
            }
        }

        private string HelpFor(string loggerProp)
        {
            var prop = FindOptionProperty(loggerProp);
            if (prop is null)
            {
                return NO_HELP;
            }

            var help = prop.GetCustomAttributes(true).OfType<HelpAttribute>()
                .FirstOrDefault()?.Help ?? NO_HELP;
            var currentValue = prop.GetValue(_logger);
            return $"{help} ({currentValue})";
        }

        private static PropertyInfo FindOptionProperty(string key)
        {
            var sanitisedKey = DeFuzzify(key);
            if (!LoggerOptionPropMap.TryGetValue(sanitisedKey, out var prop))
            {
                Warn(
                    $"Unrecognised quackers environment variable: {key} (looking for {sanitisedKey} in {string.Join(",", LoggerOptionPropMap.Keys)})"
                );
                return null;
            }

            Debug($"Quackers env var found: {key} -> {sanitisedKey}");
            return prop;
        }

        private const string NO_HELP = "(no help available)";

        private void SetLoggerProp(PropertyInfo prop, string value)
        {
            if (value is null)
            {
                return;
            }

            if (prop.PropertyType == typeof(string))
            {
                SetStringLoggerProp(prop, value);
                return;
            }


            if (prop.PropertyType == typeof(bool))
            {
                SetBooleanLoggerProp(prop, value);
                return;
            }

            if (prop.PropertyType == typeof(int))
            {
                SetIntegerLoggerProp(prop, value);
                return;
            }

            Warn($"Unhandled logger property type: {prop.PropertyType} for '{prop.Name}' - prop is ignored");
        }

        private void SetIntegerLoggerProp(PropertyInfo prop, string value)
        {
            if (int.TryParse(value, out var parsed))
            {
                prop.SetValue(_logger, parsed);
                return;
            }

            Warn($"Logger property '{prop.Name}' has type integer, but the value '{value}' was provided");
        }

        private void SetBooleanLoggerProp(PropertyInfo prop, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var isTrue = TruthyValues.Contains(value);
            var isFalse = FalsyValues.Contains(value);
            if (isTrue)
            {
                prop.SetValue(_logger, true);
            }
            else if (isFalse)
            {
                prop.SetValue(_logger, false);
            }
            else
            {
                Warn($"Invalid flag value '{value}' specified for '{prop.Name}'");
            }
        }

        private void SetStringLoggerProp(PropertyInfo prop, string value)
        {
            Debug($"Setting {prop.Name} to {value}");
            prop.SetValue(_logger, value);
        }

        private static void Warn(string str)
        {
            Console.Error.WriteLine($"WARNING: {str}".Fail());
        }

        // ReSharper disable once InconsistentNaming
        private static Action<string> Debug = _ =>
        {
        };

        private const string ENVIRONMENT_VARIABLE_PREFIX = "QUACKERS_";

        private void SetLoggerPropsFrom(Dictionary<string, string> parameters)
        {
            Debug("Dumping quackers parameters");
            foreach (var kvp in parameters)
            {
                Debug($"  {kvp.Key}: {kvp.Value}");
            }

            if (!parameters.Any())
            {
                return;
            }

            WarnForUnknownParameters(parameters);

            foreach (var prop in LoggerOptionProps)
            {
                var providedValue = TryFindParameterValueFor(parameters, prop.Name);
                if (providedValue is null)
                {
                    continue;
                }

                Debug($"Set prop: {prop.Name} to {providedValue}");
                SetLoggerProp(prop, providedValue);
            }
        }

        private string TryFindParameterValueFor(
            Dictionary<string, string> parameters,
            string propName
        )
        {
            if (parameters.TryGetValue(propName, out var providedValue))
            {
                return providedValue;
            }

            var fuzzyMatch = FindFirstFuzzyMatchedKey(parameters, propName);
            if (fuzzyMatch is null)
            {
                Debug($"Can't find prop {propName} in '{string.Join(",", parameters.Keys)}'");
                return null;
            }

            return parameters[fuzzyMatch];
        }

        private string FindFirstFuzzyMatchedKey(
            Dictionary<string, string> parameters,
            string propName
        )
        {
            var seek = DeFuzzify(propName);
            return parameters.Keys.FirstOrDefault(
                k => DeFuzzify(k) == seek
            );
        }

        private static string DeFuzzify(string str)
        {
            var result = str.StartsWith(ENVIRONMENT_VARIABLE_PREFIX)
                ? str.Substring(ENVIRONMENT_VARIABLE_PREFIX.Length)
                : str;
            return result.ToLower()
                .Replace("_", "")
                .Replace("-", "")
                .Replace(".", "");
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
                Warn(
                    $@"Valid quackers parameters are:
- {
    string.Join("\n- ", LoggerOptionPropMap.Keys.Select(k => $"{k.ToLower()} :: {LoggerOptionPropMap[k].PropertyType}"))
}
Parameters are case-insensitive. Boolean parameters can be set with values yes/no, 1/0, true/false"
                );
            }
        }

        private static readonly HashSet<string> IgnoreParameters = new(
            new[]
            {
                "TestRunDirectory",
                "TargetFramework",
                "debug"
            },
            StringComparer.OrdinalIgnoreCase
        );

        private static readonly PropertyInfo[] LoggerOptionProps = typeof(ILoggerProperties)
            .GetProperties()
            .Union(
                typeof(ILoggerProperties).GetInterfaces()
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