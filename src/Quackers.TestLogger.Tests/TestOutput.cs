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
    [TestFixture]
    public class DefaultRun
    {
        private static List<string> _stdOut = new();
        private static List<string> _stdErr = new();

        [OneTimeSetUp]
        public void Setup()
        {
            var demoProject = FindDemoProject();
            using var proc = ProcessIO.Start("dotnet", "test", "-l", "\"quackers;passlabel=[P];faillabel=[F];SKIPLABEL=[S];nocolor=true\"", demoProject);
            if (proc.Process is null)
            {
                throw new InvalidOperationException("Unable to start 'npm run demo'");
            }

            foreach (var line in proc.StandardOutput)
            {
                Console.WriteLine(line);
                _stdOut.Add(line);
            }

            foreach (var line in proc.StandardError)
            {
                Console.Error.Write(line);
                _stdErr.Add(line);
            }

            if (_stdOut.Any())
            {
                Console.WriteLine($"All stdout:\n{_stdOut.JoinWith("\n")}");
            }

            if (_stdErr.Any())
            {
                Console.WriteLine($"All stderr:\n{_stdErr.JoinWith("\n")}");
            }

            proc.Process.WaitForExit();
        }

        private string FindDemoProject()
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

        [Test]
        public void ShouldOutputPass()
        {
            // Arrange
            // Act
            Expect(_stdOut)
                .To.Contain.Exactly(1)
                .Starting.With("[P] QuackersTestHost.SomeTests.LongerPasses(1)");
            // Assert
        }

        [Test]
        public void ShouldOutputFail()
        {
            // Arrange
            Expect(_stdOut)
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
            Expect(_stdOut)
                .To.Contain.Exactly(1)
                .Starting.With("[S] QuackersTestHost.SomeTests.SkippyTesty [ skipped because... ]");
            // Assert
        }
    }
}