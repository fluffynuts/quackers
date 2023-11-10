using NUnit.Framework;
using NExpect;
using static NExpect.Expectations;

namespace Quackers.TestLogger.Tests
{
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
        }

        [TestFixture]
        public class SettingSimpleOutput
        {
            [Test]
            public void ShouldSetNoColorTrue()
            {
                // Arrange
                var sut = Create();
                // Act
                // Assert
            }
        }

        private static ILogger Create()
        {
            return new ConsoleLogger();
        }
    }
}