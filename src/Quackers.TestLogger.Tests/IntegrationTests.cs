using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PeanutButter.Utils;
using NUnit.Framework;
using NExpect;
using NUnit.Framework.Interfaces;
using static NExpect.Expectations;

namespace Quackers.TestLogger.Tests;

[TestFixture]
public class IntegrationTests
{
    private static bool Debug => DetermineIfDebug();

    private static bool DetermineIfDebug()
    {
        var envVar = Environment.GetEnvironmentVariable("DEBUG");
        if (envVar is null)
        {
            return false;
        }

        return envVar.AsBoolean();
    }

    [TestFixture]
    public class FailurePlaceholdersInsteadOfFailureIndexes
    {
        private const string SUMMARY_START = "::sum::";
        private const string SUMMARY_COMPLETE = "::end::";
        private const string FAILURE_START = "::le_fail::";
        private const string FAILURE_INDEX_PLACEHOLDER = "::#::";
        private const string SLOW_SUMMARY_START = "::slow_start::";
        private const string SLOW_SUMMARY_COMPLETE = "::slow_complete::";
        private static readonly List<string> StdOut = new();
        private static readonly List<string> StdErr = new();

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            RunTestProjectWithQuackersArgs(
                string.Join(
                    ";",
                    $"failureIndexPlaceholder={FAILURE_INDEX_PLACEHOLDER}",
                    "passlabel=[P]",
                    "faillabel=[F]",
                    "skiplabel=[S]",
                    "nonelabel=[N]",
                    "verbosesummary=true",
                    "nocolor=true",
                    "outputfailuresinline=true",
                    "nonelabel=[N]",
                    $"summarystartmarker={SUMMARY_START}",
                    $"summarycompletemarker={SUMMARY_COMPLETE}",
                    $"failurestartmarker={FAILURE_START}",
                    $"slowsummarystartmarker={SLOW_SUMMARY_START}",
                    $"slowsummarycompletemarker={SLOW_SUMMARY_COMPLETE}"
                ),
                StdOut,
                StdErr
            );
        }

        [Test]
        public void ShouldUsePlaceholderOnDemand()
        {
            // Arrange
            // Act
            var summaryBlock = FindLinesBetween(
                SUMMARY_START,
                SUMMARY_COMPLETE,
                StdOut
            );
            var line = summaryBlock.FirstOrDefault(
                l => l.Contains("QuackersTestHost.SomeTests.ShouldFail")
            );
            // Assert
            Expect(line)
                .Not.To.Be.Null(
                    "Should have a failure summary for the ShouldFail test"
                );
            Expect(line)
                .Not.To.Match("\\[\\d+\\]");
        }

        [Test]
        public void ShouldFindSlowReport()
        {
            // Arrange
            var summaryBlock = FindLinesBetween(
                SLOW_SUMMARY_START,
                SLOW_SUMMARY_COMPLETE,
                StdOut
            );
            // Act
            // Assert
            Expect(summaryBlock)
                .To.Contain.Exactly(2)
                .Matched.By("QuackersTestHost\\.SomeTests\\.LongerPasses");
        }
    }

    [TestCase("NO_COLOR", "1", true)]
    [TestCase("NO_COLOR", null, false)]
    [TestCase("NO_COLOR", "", false)]
    public void ShouldRespectEnvironmentVariable_(
        string envVar,
        string value,
        bool expected
    )
    {
        // Arrange
        using var _ = new AutoTempEnvironmentVariable(envVar, value);
        // Act

        var sut = new ConsoleLogger();
        var result = sut.NoColor;

        // Assert
        Expect(result)
            .To.Equal(expected);
    }

    [TestFixture]
    public class PrefixingTestNames
    {
        private const string SUMMARY_START = "::sum::";
        private const string SUMMARY_COMPLETE = "::end::";
        private const string FAILURE_START = "::le_fail::";
        private const string TEST_NAME_PREFIX = "Foo.Bar.";
        private static readonly List<string> StdOut = new();
        private static readonly List<string> StdErr = new();

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Started = DateTime.Now;
            RunTestProjectWithQuackersArgs(
                string.Join(
                    ";",
                    $"testnameprefix={TEST_NAME_PREFIX}",
                    "passlabel=[P]",
                    "faillabel=[F]",
                    "skiplabel=[S]",
                    "nonelabel=[N]",
                    "verbosesummary=true",
                    "nocolor=true",
                    "outputfailuresinline=true",
                    "nonelabel=[N]",
                    "showtimestamps=true",
                    $"summarystartmarker={SUMMARY_START}",
                    $"summarycompletemarker={SUMMARY_COMPLETE}",
                    $"failurestartmarker={FAILURE_START}"
                ),
                StdOut,
                StdErr
            );
            Finished = DateTime.Now;
        }

        public DateTime Started { get; set; }
        public DateTime Finished { get; set; }


        [Test]
        public void ShouldPrefixPass()
        {
            // Arrange
            // Act
            Expect(StdOut)
                .To.Contain.Exactly(1)
                .Matched.By(
                    $"^\\[P].*{TEST_NAME_PREFIX}QuackersTestHost.SomeTests.LongerPasses\\(1\\)"
                );
            // Assert
        }

        [Test]
        public void ShouldPrefixFail()
        {
            // Arrange
            Expect(StdOut)
                .To.Contain.Exactly(1)
                .Matched.By(
                    $"^\\[F].*{TEST_NAME_PREFIX}QuackersTestHost.SomeTests.ShouldFail"
                );
            // Act
            // Assert
        }

        [Test]
        public void ShouldPrefixFailSummaryPart()
        {
            // Arrange
            var block = FindLinesBetween(FAILURE_START, SUMMARY_COMPLETE, StdOut);
            // Act
            var line = block.FirstOrDefault(l => l.Contains("QuackersTestHost.SomeTests.ShouldFail"));
            // Assert
            Expect(line)
                .Not.To.Be.Null("Should have a failure summary for the failed test");
            Expect(line)
                .To.Contain(TEST_NAME_PREFIX);
        }

        [Test]
        public void ShouldPrefixSkip()
        {
            // Arrange
            // Act
            Expect(StdOut)
                .To.Contain.Exactly(1)
                .Matched.By(
                    $"^\\[S\\].*{TEST_NAME_PREFIX}QuackersTestHost.SomeTests.SkippyTesty \\[ skipped because... \\]"
                );
            // Assert
        }

        [Test]
        public void ShouldPrefixNone()
        {
            // this is how NUnit reports explicit tests
            var expected =
                $"^\\[N\\].*{TEST_NAME_PREFIX}QuackersTestHost.SomeTests.ExplicitTest \\[ integration test \\]";

            // Arrange
            Expect(StdOut)
                .To.Contain.Exactly(1)
                .Matched.By(
                    expected,
                    () => $@"Looking for:
{expected}
But explicit test line is:
{StdOut.FirstOrDefault(s => s.Contains("ExplicitTest"))}"
                );
            // Act
            // Assert
        }

        [Test]
        public void ShouldIncludeTimestampsWhenOptingIn()
        {
            // Arrange
            var pass = new Regex("^\\[P\\]");
            var dtRegex = new Regex("^\\[[A-Z]{1}\\] \\[(?<timestamp>[0-9:. -]+)\\]");
            var timestampRegex = new Regex(
                string.Join(
                    "",
                    "^(?<year>[0-9]{4})",
                    "-",
                    "(?<month>[0-9]{2})",
                    "-",
                    "(?<day>[0-9]{2})",
                    " ",
                    "(?<hour>[0-9]{2})",
                    ":",
                    "(?<minute>[0-9]{2})",
                    ":",
                    "(?<second>[0-9]{2})",
                    ".",
                    "(?<ms>[0-9]{3})$"
                )
            );
            // Act
            var passLine = StdOut.FirstOrDefault(pass.IsMatch);
            // Assert
            Expect(passLine)
                .Not.To.Be.Null();
            var dtMatch = dtRegex.Match(passLine);
            Expect(dtMatch.Success)
                .To.Be.True(
                    () => $"Should find a timestamp in the log line:\n{passLine}"
                );
            var timestampValue = dtMatch.Groups["timestamp"].Value;
            Expect(timestampValue)
                .Not.To.Be.Null.Or.Empty();
            var tm = timestampRegex.Match(timestampValue);
            Expect(tm.Success)
                .To.Be.True();
            var timestamp = new DateTime(
                intVal("year"),
                intVal("month"),
                intVal("day"),
                intVal("hour"),
                intVal("minute"),
                intVal("second"),
                intVal("ms")
            );

            Expect(timestamp)
                .To.Be.Greater.Than.Or.Equal.To(Started)
                .And
                .To.Be.Less.Than.Or.Equal.To(Finished);

            int intVal(string groupName)
            {
                return tm.Groups[groupName].Value.AsInteger();
            }
        }
    }

    [TestFixture]
    public class Bugs
    {
        private const string SUMMARY_START = "::sum::";
        private const string SUMMARY_COMPLETE = "::end::";
        private const string FAILURE_START = "::le_fail::";
        private const string TEST_NAME_PREFIX = "Foo.Bar.";
        private static readonly List<string> StdOut = new();
        private static readonly List<string> StdErr = new();

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Started = DateTime.Now;
            RunTestProjectWithQuackersArgs(
                string.Join(
                    ";",
                    $"testnameprefix={TEST_NAME_PREFIX}",
                    "passlabel=[P]",
                    "faillabel=[F]",
                    "skiplabel=[S]",
                    "nonelabel=[N]",
                    "verbosesummary=true",
                    "nocolor=true",
                    "outputfailuresinline=true",
                    "nonelabel=[N]",
                    "showtimestamps=true",
                    $"summarystartmarker={SUMMARY_START}",
                    $"summarycompletemarker={SUMMARY_COMPLETE}",
                    $"failurestartmarker={FAILURE_START}"
                ),
                StdOut,
                StdErr,
                new Dictionary<string, string>()
                {
                    ["FORCE_PASS"] = "true",
                }
            );
            Finished = DateTime.Now;
        }

        public DateTime Started { get; set; }
        public DateTime Finished { get; set; }

        [Test]
        public void ShouldIncludeVerboseSummaryOnRequestEvenWhenNoFails()
        {
            // Arrange
            var lines = FindLinesBetween(SUMMARY_START, SUMMARY_COMPLETE, StdOut);
            // Act
            Expect(lines)
                .To.Contain.Exactly(1)
                .Matched.By("Test results:");
            Expect(lines)
                .To.Contain.Exactly(1)
                .Matched.By("Passed:");
            Expect(lines)
                .To.Contain.Exactly(1)
                .Matched.By("Failed:");
            Expect(lines)
                .To.Contain.Exactly(1)
                .Matched.By("Total:");
            // Assert
        }
    }

    public class TimestampFormatProvider : IFormatProvider
    {
        public object GetFormat(Type formatType)
        {
            throw new NotImplementedException();
        }
    }

    [TestFixture]
    public class ZarroUsage
    {
        private static readonly List<string> StdOut = new();
        private static readonly List<string> StdErr = new();

        private const string LOG_PREFIX = "::";
        private const string SUMMARY_START = "::SS::";
        private const string SUMMARY_COMPLETE = "::SC::";
        private const string FAILURE_START = "::SF::";
        private const string SLOW_START = "::SSS::";
        private const string SLOW_COMPLETE = "::SSC::";
        private const string FAILURE_INDEX_PLACEHOLDER = "::[#]::";
        private const string SLOW_INDEX_PLACEHOLDER = "::[-]::";
        private const string SHOW_TOTALS_START = "::VV::";
        private const string SHOW_TOTALS_COMPLETE = "::vv::";


        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            RunTestProjectWithQuackersArgs(
                string.Join(
                    ";",
                    $"logprefix={LOG_PREFIX}",
                    "passlabel=[P]",
                    "faillabel=[F]",
                    "skiplabel=[S]",
                    "nonelabel=[N]",
                    "showTotals=true",
                    "nocolor=true",
                    "showtimestamps=false",
                    "outputfailuresinline=true",
                    "showtimestamps=false",
                    $"summarystartmarker={SUMMARY_START}",
                    $"summarycompletemarker={SUMMARY_COMPLETE}",
                    $"failurestartmarker={FAILURE_START}",
                    $"slowsummarystartmarker={SLOW_START}",
                    $"slowsummarycompletemarker={SLOW_COMPLETE}",
                    $"failureindexplaceholder={FAILURE_INDEX_PLACEHOLDER}",
                    $"slowindexplaceholder={SLOW_INDEX_PLACEHOLDER}",
                    $"summaryTotalsStartMarker={SHOW_TOTALS_START}",
                    $"summaryTotalsCompleteMarker={SHOW_TOTALS_COMPLETE}"
                ),
                StdOut,
                StdErr
            );
        }

        [Test]
        public void ShouldNotWarn()
        {
            // Arrange
            // Act
            Expect(StdOut)
                .Not.To.Contain.Any
                .Matched.By(s => s.Contains("WARNING:"));
            Expect(StdErr)
                .Not.To.Contain.Any
                .Matched.By(s => s.Contains("WARNING:"));
            // Assert
        }

        [Test]
        public void ShouldPrefixQuackersLines()
        {
            // Arrange
            // Act
            Expect(StdOut)
                .To.Contain.At.Least(10)
                .Matched.By(s => s.StartsWith(LOG_PREFIX));
            // Assert
        }

        [Test]
        public void ShouldIncludeSummaryBlock()
        {
            // Arrange
            // Act
            Expect(StdOut)
                .To.Contain.Exactly(1)
                .Starting.With($"{LOG_PREFIX}{SUMMARY_START}");
            Expect(StdOut)
                .To.Contain.Exactly(1)
                .Starting.With($"{LOG_PREFIX}{SUMMARY_COMPLETE}");
            var summaryBlock =
                FindLinesBetween($"{LOG_PREFIX}{SHOW_TOTALS_START}", $"{LOG_PREFIX}{SHOW_TOTALS_COMPLETE}", StdOut)
                    .Where(line => line.Length > LOG_PREFIX.Length)
                    .Select(line => line.Substring(LOG_PREFIX.Length))
                    .ToArray();
            Expect(summaryBlock)
                .To.Contain.Exactly(1)
                .Matched.By(s => s.Trim().StartsWith("Passed:"));
            Expect(summaryBlock)
                .To.Contain.Exactly(1)
                .Matched.By(s => s.Trim().StartsWith("Failed:"));
            Expect(summaryBlock)
                .To.Contain.Exactly(1)
                .Matched.By(s => s.Trim().StartsWith("Skipped:"));
            Expect(summaryBlock)
                .To.Contain.Exactly(1)
                .Matched.By(s => s.Trim().StartsWith("Total:"));
            Expect(summaryBlock)
                .To.Contain.Exactly(1)
                .Matched.By(s => s.Trim().StartsWith("Run time:"));
            // Assert
        }

        [Test]
        public void ShouldProvideFailureStartMarker()
        {
            // Arrange
            // Act
            var block = FindLinesBetween($"{LOG_PREFIX}{SUMMARY_START}", $"{LOG_PREFIX}{SUMMARY_COMPLETE}", StdOut);
            Expect(block)
                .To.Contain.Exactly(1)
                .Equal.To($"{LOG_PREFIX}{FAILURE_START}");
            Expect(block)
                .To.Contain.None
                .Matched.By(
                    s => s.Contains("Failures:"),
                    $"Should excluded the Failures label when the {nameof(ILogger.FailureStartMarker)} prop is set"
                );
            // Assert
        }

        [Test]
        public void ShouldIncludeFailureOutputOnDemand()
        {
            // Arrange
            // Act
            var testLine = StdOut
                .Where(s => s.StartsWith(LOG_PREFIX))
                .FirstOrDefault(s => s.Contains($"{LOG_PREFIX}[F] QuackersTestHost.SomeTests.ShouldFail"));
            Expect(testLine)
                .Not.To.Be.Null();
            var interesting = FindLinesBetween(
                s => s.StartsWith(testLine),
                s => s.Contains(SUMMARY_START),
                StdOut
            );
            Expect(interesting)
                .To.Contain.Exactly(1)
                .Containing("this test should fail");
            // Assert
        }
    }

    [TestFixture]
    public class DefaultRun
    {
        private static readonly List<string> StdOut = new();
        private static readonly List<string> StdErr = new();

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            RunTestProjectWithQuackersArgs(
                "passlabel=[P];faillabel=[F];SKIPLABEL=[S];NoneLabel=[N];nocolor=true;showtimestamps=false",
                StdOut,
                StdErr
            );
        }

        [Test]
        public void ShouldOutputPass()
        {
            // Arrange
            // Act
            Expect(StdOut)
                .To.Contain.Exactly(1)
                .Starting.With(
                    "[P] QuackersTestHost.SomeTests.LongerPasses(1)",
                    () => $"full output:\n{StdOut.JoinWith("\n")}"
                );
            // Assert
        }

        [Test]
        public void ShouldOutputFail()
        {
            // Arrange
            Expect(StdOut)
                .To.Contain.Exactly(1)
                .Starting.With(
                    "[F] QuackersTestHost.SomeTests.ShouldFail",
                    () => $"full output:\n{StdOut.JoinWith("\n")}"
                );
            // Act
            // Assert
        }

        [Test]
        public void ShouldOutputSkip()
        {
            // Arrange
            // Act
            Expect(StdOut)
                .To.Contain.Exactly(1)
                .Starting.With(
                    "[S] QuackersTestHost.SomeTests.SkippyTesty [ skipped because... ]",
                    () => $"full output:\n{StdOut.JoinWith("\n")}"
                );
            // Assert
        }

        [Test]
        public void ShouldOutputNone()
        {
            // this is how NUnit reports explicit tests
            // Arrange
            Expect(StdOut)
                .To.Contain.Exactly(1)
                .Starting.With(
                    "[N] QuackersTestHost.SomeTests.ExplicitTest [ integration test ]",
                    () => $"full output:\n{StdOut.JoinWith("\n")}"
                );
            // Act
            // Assert
        }
    }

    private static void RunTestProjectWithQuackersArgs(
        string qargs,
        List<string> stdout,
        List<string> stderr,
        Dictionary<string, string> environment = null
    )
    {
        var demoProject = FindDemoProject();
        var env = CreateQuackersCleansedEnvironment();
        if (environment is not null)
        {
            foreach (var kvp in environment)
            {
                env[kvp.Key] = kvp.Value;
            }
        }

        using var proc = ProcessIO
            .WithEnvironment(env)
            .Start(
                "dotnet",
                "test",
                demoProject,
                "-v",
                "q",
                "-l",
                $"\"quackers;{qargs}\""
            );
        var cl = $"dotnet test {demoProject} -v -q -l \"quackers;{qargs}\"";
        if (proc.Process is null)
        {
            throw new InvalidOperationException("Unable to start 'npm run demo'");
        }

        foreach (var line in proc.StandardOutput)
        {
            stdout.Add(line);
        }

        foreach (var line in proc.StandardError)
        {
            stderr.Add(line);
        }

        if (Debug)
        {
            if (stdout.Any())
            {
                Console.Error.WriteLine($"All stdout:\n{stdout.JoinWith("\n")}");
            }

            if (stderr.Any())
            {
                Console.Error.WriteLine($"All stderr:\n{stderr.JoinWith("\n")}");
            }
        }

        proc.Process.WaitForExit();
    }

    private static Dictionary<string, string> CreateQuackersCleansedEnvironment()
    {
        var env = new Dictionary<string, string>();
        foreach (DictionaryEntry e in Environment.GetEnvironmentVariables())
        {
            var varName = $"{e.Key}";
            if (varName.StartsWith("QUACKERS_", StringComparison.OrdinalIgnoreCase))
            {
                env[varName] = "";
            }
        }

        return env;
    }

    private static string FindDemoProject()
    {
        var asmPath = new Uri(typeof(TestOutput).Assembly.Location).LocalPath;
        var testDir = Path.GetDirectoryName(asmPath);
        while (testDir is not null)
        {
            var dirs = Directory.GetDirectories(testDir);
            if (dirs.Any(d => Path.GetFileName(d) == "Demo"))
            {
                var result = Path.Combine(testDir, "Demo", "Demo.csproj");
                if (!File.Exists(result))
                {
                    throw new InvalidOperationException($"Demo project not found at '{result}'");
                }

                return result;
            }

            testDir = Path.GetDirectoryName(testDir);
        }

        throw new InvalidOperationException("Can't find the demo project");
    }

    private static string[] FindLinesBetween(
        string start,
        string end,
        IEnumerable<string> lines
    )
    {
        return FindLinesBetween(
            s => s.StartsWith(start),
            s => s.StartsWith(end),
            lines
        );
    }

    private static string[] FindLinesBetween(
        Func<string, bool> startMatcher,
        Func<string, bool> endMatcher,
        IEnumerable<string> lines
    )
    {
        var result = new List<string>();
        var inBlock = false;
        foreach (var line in lines)
        {
            if (startMatcher(line))
            {
                inBlock = true;
                continue;
            }

            if (endMatcher(line))
            {
                inBlock = false;
                continue;
            }

            if (inBlock)
            {
                result.Add(line);
            }
        }

        return result.ToArray();
    }
}