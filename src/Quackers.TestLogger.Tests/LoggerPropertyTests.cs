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
        [TestCase("NotFoundLabel", typeof(string))]
        [TestCase("NoColor", typeof(bool))]
        [TestCase("VerboseSummary", typeof(bool))]
        [TestCase("SummaryStartMarker", typeof(string))]
        [TestCase("SummaryCompleteMarker", typeof(string))]
        [TestCase("FailureStartMarker", typeof(string))]
        [TestCase("LogPrefix", typeof(string))]
        [TestCase("OutputFailuresInline", typeof(bool))]
        [TestCase("TestNamePrefix", typeof(string))]
        [TestCase("FailureIndexPlaceholder", typeof(string))]
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