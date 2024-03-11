using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Framework;
using NExpect;
using static NExpect.Expectations;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace Quackers.TestLogger.Tests;

[TestFixture]
public class ConsoleLoggerTests
{
    [TestFixture]
    public class Defaults
    {
        [TestCase("✅")]
        public void ShouldDefaultPassLabelTo_(string expected)
        {
            // Arrange
            var sut = Create();
            // Act
            var result = sut.PassLabel;
            // Assert
            Expect(result)
                .To.Equal(expected);
        }

        [TestCase("🛑")]
        public void ShouldDefaultFailLabelTo_(string expected)
        {
            // Arrange
            var sut = Create();
            // Act
            var result = sut.FailLabel;
            // Assert
            Expect(result)
                .To.Equal(expected);
        }

        [TestCase("❓")]
        public void ShouldDefaultNoneLabelTo_(string expected)
        {
            // Arrange
            var sut = Create();
            // Act
            var result = sut.NoneLabel;
            // Assert
            Expect(result)
                .To.Equal(expected);
        }

        [TestCase("🚫")]
        public void ShouldDefaultSkipLabelTo_(string expected)
        {
            // Arrange
            var sut = Create();
            // Act
            var result = sut.SkipLabel;
            // Assert
            Expect(result)
                .To.Equal(expected);
        }

        [TestCase("🤷")]
        public void ShouldDefaultNotFoundLabelTo_(string expected)
        {
            // Arrange
            var sut = Create();
            // Act
            var result = sut.NotFoundLabel;
            // Assert
            Expect(result)
                .To.Equal(expected);
        }

        [Test]
        public void ShouldDefaultNoColorOff()
        {
            // Arrange
            var sut = Create();
            // Act
            var result = sut.NoColor;
            // Assert
            Expect(result)
                .To.Be.False();
        }

        [Test]
        public void ShouldDefaultLogPrefixNull()
        {
            // Arrange
            var sut = Create();
            // Act
            var result = sut.LogPrefix;
            // Assert
            Expect(result)
                .To.Be.Null();
        }

        // The following are used by consumers who
        // have set (probably) a logging prefix and want to
        // provide a "pure quackers" experience to
        // the observer; without filtering, there would
        // be a summary from quackers _and_ from the
        // default-installed console logger, which
        // apparently can't be switched off
        // - VerboseSummary
        // - OutputFailuresInline
        // - SummaryStartMarker
        // - SummaryCompleteMarker
        // - FailureStartMarker
        // - TestNamePrefix
        // - FailureIndexPlaceholder

        [Test]
        public void ShouldDefaultVerboseSummaryOff()
        {
            // Arrange
            var sut = Create();
            // Act
            var result = sut.ShowTotals;
            // Assert
            Expect(result)
                .To.Be.False();
        }

        [Test]
        public void ShouldDefaultInlineFailuresOff()
        {
            // Arrange
            var sut = Create();
            // Act
            var result = sut.OutputFailuresInline;
            // Assert
            Expect(result)
                .To.Be.False();
        }

        [Test]
        public void ShouldDefaultSummaryStartMarkerToNull()
        {
            // Arrange
            var sut = Create();
            // Act
            var result = sut.SummaryStartMarker;
            // Assert
            Expect(result)
                .To.Be.Null();
        }

        [Test]
        public void ShouldDefaultSummaryCompleteMarkerToNull()
        {
            // Arrange
            var sut = Create();
            // Act
            var result = sut.SummaryCompleteMarker;
            // Assert
            Expect(result)
                .To.Be.Null();
        }

        [Test]
        public void ShouldDefaultTestNamePrefixToNull()
        {
            // Arrange
            var sut = Create();
            // Act
            var result = sut.TestNamePrefix;
            // Assert
            Expect(result)
                .To.Be.Null();
        }

        [Test]
        public void ShouldDefaultFailureIndexPlaceholderToNull()
        {
            // Arrange
            var sut = Create();
            // Act
            var result = sut.FailureIndexPlaceholder;
            // Assert
            Expect(result)
                .To.Be.Null();
        }

        [Test]
        public void ShouldDefaultShowHelpTrue()
        {
            // Arrange
            var sut = Create();
            // Act
            var result = sut.ShowHelp;
            // Assert
            Expect(result)
                .To.Be.True();
        }

        [Test]
        public void ShouldDefaultMaxSlowTestsTo10()
        {
            // Arrange
            var sut = Create();
            // Act
            var result = sut.MaxSlowTestsToDisplay;
            // Assert
            Expect(result)
                .To.Equal(10);
        }
    }

    [TestFixture]
    public class SlowTestOutput
    {
        [Test]
        public void ShouldObserveLimit()
        {
            // Arrange
            var sut = Create();
            sut.MaxSlowTestsToDisplay = 2;
            var events = GetRandomArray<TestResultEventArgs>(3, 5);
            foreach (var ev in events)
            {
                sut.StoreSlowTest(ev);
            }

            // Act
            sut.PrintSlowTests();
            // Assert
            var collected = new List<string>();
            var inSlowTestSummary = false;
            foreach (var line in sut.StdOut)
            {
                if (line == sut.SlowSummaryStartMarker)
                {
                    inSlowTestSummary = true;
                    continue;
                }
                if (line == sut.SlowSummaryCompleteMarker)
                {
                    break;
                }

                if (!inSlowTestSummary)
                {
                    continue;
                }
                collected.Add(line);
            }
            
            Expect(collected)
                .To.Contain.Only(2)
                .Items();
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        private static MyConsoleLogger Create()
        {
            return new()
            {
                HighlightSlowTests = true,
                SlowSummaryStartMarker = ">>> slow summary start <<<",
                SlowSummaryCompleteMarker = "<<< slow summary end >>>"
            };
        }
    }

    public class MyConsoleLogger : ConsoleLogger
    {
        public List<string> StdErr { get; } = new();
        public List<string> StdOut { get; } = new();

        protected override void LogToStdErr(string str)
        {
            StdErr.Add(str);
        }

        protected override void LogToStdout(string str)
        {
            StdOut.Add(str);
        }

        public void StoreSlowTest(TestResultEventArgs args)
        {
            StoreSlow(args);
        }

        public new void PrintSlowTests()
        {
            base.PrintSlowTests();
        }
    }

    private static ILogger Create()
    {
        return new ConsoleLogger();
    }
}