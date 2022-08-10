using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PeanutButter.Utils;
using NUnit.Framework;
using NExpect;
using NUnit.Framework.Interfaces;
using static NExpect.Expectations;

namespace Quackers.TestLogger.Tests;

public class Tests
{
    private const bool DEBUG = false;

    [TestFixture]
    public class ZarroUsage
    {
        private static readonly List<string> StdOut = new();
        private static readonly List<string> StdErr = new();

        private const string LOG_PREFIX = "::quackers::";
        private const string SUMMARY_START = "<summary>";
        private const string SUMMARY_COMPLETE = "</summary>";

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
                    "nonelabel=[N]",
                    $"summarystartmarker={SUMMARY_START}",
                    $"summarycompletemarker={SUMMARY_COMPLETE}"
                ),
                StdOut, StdErr);
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
            var summaryBlock = FindLinesBetween($"{LOG_PREFIX}{SUMMARY_START}", $"{LOG_PREFIX}{SUMMARY_COMPLETE}", StdOut)
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
        using var proc = ProcessIO.Start("dotnet", "test", "-l",
            $"\"quackers;{qargs}\"", demoProject);
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

    private static string[] FindLinesBetween(string start, string end, IEnumerable<string> lines)
    {
        var result = new List<string>();
        var inBlock = false;
        foreach (var line in lines)
        {
            if (line.StartsWith(start))
            {
                inBlock = true;
                continue;
            }

            if (line.StartsWith(end))
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