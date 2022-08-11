using System;
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
    private const bool DEBUG = false;

    [TestFixture]
    public class FailurePlaceholdersInsteadOfFailureIndexes
    {
        private const string SUMMARY_START = "::sum::";
        private const string SUMMARY_COMPLETE = "::end::";
        private const string FAILURE_START = "::le_fail::";
        private const string FAILURE_INDEX_PLACEHOLDER = "::#::";
        private static readonly List<string> StdOut = new();
        private static readonly List<string> StdErr = new();

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            RunTestProjectWithQuackersArgs(
                string.Join(";",
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
                    $"failurestartmarker={FAILURE_START}"
                ), StdOut, StdErr
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
                .Not.To.Match(new Regex("\\[\\d+\\]"));
        }
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
            RunTestProjectWithQuackersArgs(
                string.Join(";",
                    $"testnameprefix={TEST_NAME_PREFIX}",
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
                    $"failurestartmarker={FAILURE_START}"
                ), StdOut, StdErr
            );
        }

        [Test]
        public void ShouldPrefixPass()
        {
            // Arrange
            // Act
            Expect(StdOut)
                .To.Contain.Exactly(1)
                .Starting.With($"[P] {TEST_NAME_PREFIX}QuackersTestHost.SomeTests.LongerPasses(1)");
            // Assert
        }

        [Test]
        public void ShouldPrefixFail()
        {
            // Arrange
            Expect(StdOut)
                .To.Contain.Exactly(1)
                .Starting.With($"[F] {TEST_NAME_PREFIX}QuackersTestHost.SomeTests.ShouldFail");
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
                .Starting.With($"[S] {TEST_NAME_PREFIX}QuackersTestHost.SomeTests.SkippyTesty [ skipped because... ]");
            // Assert
        }

        [Test]
        public void ShouldPrefixNone()
        {
            // this is how NUnit reports explicit tests
            var expected = $"[N] {TEST_NAME_PREFIX}QuackersTestHost.SomeTests.ExplicitTest [ integration test ]";
            // Arrange
            Expect(StdOut)
                .To.Contain.Exactly(1)
                .Starting.With(
                    expected,
                    () => $@"Looking for:
{expected}
But explicit test line is:
{StdOut.FirstOrDefault(s => s.Contains("ExplicitTest"))}"
                );
            // Act
            // Assert
        }
    }

    [TestFixture]
    public class ZarroUsage
    {
        private static readonly List<string> StdOut = new();
        private static readonly List<string> StdErr = new();

        private const string LOG_PREFIX = "::quackers::";
        private const string SUMMARY_START = "<summary>";
        private const string SUMMARY_COMPLETE = "</summary>";
        private const string FAILURE_START = "-- failures --";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            RunTestProjectWithQuackersArgs(
                string.Join(";",
                    $"logprefix={LOG_PREFIX}",
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
                    $"failurestartmarker={FAILURE_START}"
                ),
                StdOut, StdErr);
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
                FindLinesBetween($"{LOG_PREFIX}{SUMMARY_START}", $"{LOG_PREFIX}{SUMMARY_COMPLETE}", StdOut)
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
                .FirstOrDefault(s => s.Contains("[F] QuackersTestHost.SomeTests.ShouldFail"));
            Expect(testLine)
                .Not.To.Be.Null();
            var interesting = FindLinesBetween(
                s => s.StartsWith(testLine),
                s => !s.StartsWith(LOG_PREFIX),
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
            RunTestProjectWithQuackersArgs("passlabel=[P];faillabel=[F];SKIPLABEL=[S];NoneLabel=[N];nocolor=true",
                StdOut, StdErr);
        }

        [Test]
        public void ShouldOutputPass()
        {
            // Arrange
            // Act
            Expect(StdOut)
                .To.Contain.Exactly(1)
                .Starting.With("[P] QuackersTestHost.SomeTests.LongerPasses(1)");
            // Assert
        }

        [Test]
        public void ShouldOutputFail()
        {
            // Arrange
            Expect(StdOut)
                .To.Contain.Exactly(1)
                .Starting.With("[F] QuackersTestHost.SomeTests.ShouldFail");
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
                .Starting.With("[S] QuackersTestHost.SomeTests.SkippyTesty [ skipped because... ]");
            // Assert
        }

        [Test]
        public void ShouldOutputNone()
        {
            // this is how NUnit reports explicit tests
            // Arrange
            Expect(StdOut)
                .To.Contain.Exactly(1)
                .Starting.With("[N] QuackersTestHost.SomeTests.ExplicitTest [ integration test ]");
            // Act
            // Assert
        }
    }

    private static void RunTestProjectWithQuackersArgs(string qargs, List<string> stdout, List<string> stderr)
    {
        var demoProject = FindDemoProject();
        using var proc = ProcessIO.Start("dotnet", "test", demoProject, "-l",
            $"quackers;{qargs}");
        if (proc.Process is null)
        {
            throw new InvalidOperationException("Unable to start 'npm run demo'");
        }

        foreach (var line in proc.StandardOutput)
        {
            Console.WriteLine(line);
            stdout.Add(line);
        }

        foreach (var line in proc.StandardError)
        {
            Console.Error.Write(line);
            stderr.Add(line);
        }

        if (DEBUG)
#pragma warning disable CS0162
        {
            if (stdout.Any())
            {
                Console.WriteLine($"All stdout:\n{stdout.JoinWith("\n")}");
            }

            if (stderr.Any())
            {
                Console.WriteLine($"All stderr:\n{stderr.JoinWith("\n")}");
            }
        }
#pragma warning restore CS0162

        proc.Process.WaitForExit();
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