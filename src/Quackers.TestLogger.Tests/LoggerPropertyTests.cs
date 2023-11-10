using System;
using NUnit.Framework;
using NExpect;
using static NExpect.Expectations;

namespace Quackers.TestLogger.Tests
{
    [TestFixture]
    public class LoggerPropertyTests
    {
        [TestCase("PassLabel", typeof(string))]
        [TestCase("FailLabel", typeof(string))]
        [TestCase("NoneLabel", typeof(string))]
        [TestCase("SkipLabel", typeof(string))]
        [TestCase("NotFoundLabel", typeof(string))]
        
        [TestCase("NoColor", typeof(bool))]
        
        [TestCase("Theme", typeof(string))]
        
        [TestCase("HighlightSlowTests", typeof(bool))]
        [TestCase("SlowTestThresholdMs", typeof(int))]
        
        [TestCase("DebugLogFile", typeof(string))]
        
        [TestCase("ShowTotals", typeof(bool))]
        [TestCase("OutputFailuresInline", typeof(bool))]
        [TestCase("ShowHelp", typeof(bool))]
        
        [TestCase("SummaryStartMarker", typeof(string))]
        [TestCase("SummaryCompleteMarker", typeof(string))]
        [TestCase("SummaryTotalsStartMarker", typeof(string))]
        [TestCase("SummaryTotalsCompleteMarker", typeof(string))]
        [TestCase("FailureStartMarker", typeof(string))]
        [TestCase("SlowSummaryStartMarker", typeof(string))]
        [TestCase("SlowSummaryCompleteMarker", typeof(string))]
        [TestCase("LogPrefix", typeof(string))]
        [TestCase("TestNamePrefix", typeof(string))]
        [TestCase("FailureIndexPlaceholder", typeof(string))]
        [TestCase("SlowIndexPlaceholder", typeof(string))]
        public void ShouldHaveProperty_(string name, Type type)
        {
            // Arrange
            var sut = typeof(ILoggerProperties);
            // Act
            Expect(sut)
                .To.Have.Property(name)
                .With.Type(type);
            // Assert
        }
    }
}