using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Framework;
using NExpect;
using PeanutButter.Utils;
using static NExpect.Expectations;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace Quackers.TestLogger.Tests;

[TestFixture]
public class LoggerTests
{
    [Test]
    public void ShouldImplement_ITestLoggerWithParameters()
    {
        // Arrange
        var sut = typeof(Logger);
        // Act
        Expect(sut)
            .To.Implement<ITestLoggerWithParameters>();
        // Assert
    }

    [TestFixture]
    public class Configuration
    {
        [TestFixture]
        public class ViaEnvironment
        {
            public static IEnumerable<(string environmentVariable, string property)>
                EnvironmentConfigTestCases()
            {
                foreach (var prop in typeof(ILoggerProperties).GetProperties())
                {
                    yield return DirectUpper(prop);
                    yield return DirectLower(prop);
                    yield return DirectRandom(prop);

                    yield return SnakeUpper(prop);
                    yield return SnakeLower(prop);
                    yield return SnakeRandom(prop);
                }

                (string environmentVariable, string property) DirectUpper(PropertyInfo prop)
                {
                    return ($"QUACKERS_{prop.Name.ToUpper()}", prop.Name);
                }

                (string environmentVariable, string property) DirectLower(PropertyInfo prop)
                {
                    return ($"QUACKERS_{prop.Name.ToLower()}", prop.Name);
                }

                (string environmentVariable, string property) DirectRandom(PropertyInfo prop)
                {
                    return ($"QUACKERS_{prop.Name.ToRandomCase()}", prop.Name);
                }

                (string environmentVariable, string property) SnakeUpper(PropertyInfo prop)
                {
                    return ($"QUACKERS_{prop.Name.ToSnakeCase().ToUpper()}", prop.Name);
                }

                (string environmentVariable, string property) SnakeLower(PropertyInfo prop)
                {
                    return ($"QUACKERS_{prop.Name.ToSnakeCase().ToLower()}", prop.Name);
                }

                (string environmentVariable, string property) SnakeRandom(PropertyInfo prop)
                {
                    return ($"QUACKERS_{prop.Name.ToSnakeCase().ToLower()}", prop.Name);
                }

                (string environmentVariable, string property) KebabUpper(PropertyInfo prop)
                {
                    return ($"QUACKERS_{prop.Name.ToKebabCase().ToUpper()}", prop.Name);
                }

                (string environmentVariable, string property) KebabLower(PropertyInfo prop)
                {
                    return ($"QUACKERS_{prop.Name.ToKebabCase().ToLower()}", prop.Name);
                }

                (string environmentVariable, string property) KebabRandom(PropertyInfo prop)
                {
                    return ($"QUACKERS_{prop.Name.ToKebabCase().ToLower()}", prop.Name);
                }

                (string environmentVariable, string property) DotUpper(PropertyInfo prop)
                {
                    return ($"QUACKERS_{prop.Name.ToKebabCase().Replace("-", ".").ToUpper()}", prop.Name);
                }

                (string environmentVariable, string property) DotLower(PropertyInfo prop)
                {
                    return ($"QUACKERS_{prop.Name.ToKebabCase().Replace("-", ".").ToLower()}", prop.Name);
                }

                (string environmentVariable, string property) DotRandom(PropertyInfo prop)
                {
                    return ($"QUACKERS_{prop.Name.ToKebabCase().Replace("-", ".").ToLower()}", prop.Name);
                }
            }

            [TestCaseSource(nameof(EnvironmentConfigTestCases))]
            public void ShouldConfigureConsoleLoggerFromEnvironment(
                (string envVar, string propertyName) testCase
            )
            {
                // Arrange
                var (envVar, propertyName) = testCase;
                var prop = typeof(ILoggerProperties)
                    .GetProperty(propertyName);
                Expect(prop)
                    .Not.To.Be.Null();
                var expected = GetRandom(prop!.PropertyType);
                var sut = Create();
                var events = new TestEvents();
                var parameters = new Dictionary<string, string>();
                // Act
                using var _ = new AutoTempEnvironmentVariable(
                    envVar,
                    $"{expected}"
                );
                sut.Initialize(events, parameters);

                // Assert
                var consoleLogger = sut.ConsoleLogger();
                Expect(consoleLogger)
                    .Not.To.Be.Null();
                var propValue = prop.GetValue(consoleLogger);
                Expect(propValue)
                    .To.Equal(expected);
            }
        }

        [TestFixture]
        public class ViaCommandlineParameter
        {
            public static IEnumerable<(string parameterName, string propertyName)>
                EnvironmentConfigTestCases()
            {
                foreach (var prop in typeof(ILoggerProperties).GetProperties())
                {
                    yield return DirectUpper(prop);
                    yield return DirectLower(prop);
                    yield return DirectRandom(prop);

                    yield return SnakeUpper(prop);
                    yield return SnakeLower(prop);
                    yield return SnakeRandom(prop);
                }

                (string parameterName, string propertyName) DirectUpper(PropertyInfo prop)
                {
                    return (prop.Name.ToUpper(), prop.Name);
                }

                (string parameterName, string propertyName) DirectLower(PropertyInfo prop)
                {
                    return (prop.Name.ToLower(), prop.Name);
                }

                (string parameterName, string propertyName) DirectRandom(PropertyInfo prop)
                {
                    return (prop.Name.ToRandomCase(), prop.Name);
                }

                (string parameterName, string propertyName) SnakeUpper(PropertyInfo prop)
                {
                    return (prop.Name.ToSnakeCase().ToUpper(), prop.Name);
                }

                (string parameterName, string propertyName) SnakeLower(PropertyInfo prop)
                {
                    return (prop.Name.ToSnakeCase().ToLower(), prop.Name);
                }

                (string parameterName, string propertyName) SnakeRandom(PropertyInfo prop)
                {
                    return (prop.Name.ToSnakeCase().ToLower(), prop.Name);
                }

                (string parameterName, string propertyName) KebabUpper(PropertyInfo prop)
                {
                    return (prop.Name.ToKebabCase().ToUpper(), prop.Name);
                }

                (string parameterName, string propertyName) KebabLower(PropertyInfo prop)
                {
                    return (prop.Name.ToKebabCase().ToLower(), prop.Name);
                }

                (string parameterName, string propertyName) KebabRandom(PropertyInfo prop)
                {
                    return (prop.Name.ToKebabCase().ToLower(), prop.Name);
                }

                (string parameterName, string propertyName) DotUpper(PropertyInfo prop)
                {
                    return (prop.Name.ToKebabCase().Replace("-", ".").ToUpper(), prop.Name);
                }

                (string parameterName, string propertyName) DotLower(PropertyInfo prop)
                {
                    return (prop.Name.ToKebabCase().Replace("-", ".").ToLower(), prop.Name);
                }

                (string parameterName, string propertyName) DotRandom(PropertyInfo prop)
                {
                    return (prop.Name.ToKebabCase().Replace("-", "-").ToLower(), prop.Name);
                }
            }

            [TestCaseSource(nameof(EnvironmentConfigTestCases))]
            public void ShouldConfigureConsoleLoggerFromEnvironment(
                (string parameterName, string propertyName) testCase
            )
            {
                // Arrange
                var (envVar, propertyName) = testCase;
                var prop = typeof(ILoggerProperties)
                    .GetProperty(propertyName);
                Expect(prop)
                    .Not.To.Be.Null();
                var expected = GetRandom(prop!.PropertyType);
                var parameters = new Dictionary<string, string>()
                {
                    [envVar] = $"{expected}"
                };
                var sut = Create();
                var events = new TestEvents();

                // Act
                sut.Initialize(events, parameters);

                // Assert
                var consoleLogger = sut.ConsoleLogger();
                Expect(consoleLogger)
                    .Not.To.Be.Null();
                var propValue = prop.GetValue(consoleLogger);
                Expect(propValue)
                    .To.Equal(expected);
            }
        }
    }

    public class TestEvents : TestLoggerEvents
    {
#pragma warning disable CS0067
        public override event EventHandler<TestRunMessageEventArgs> TestRunMessage;
        public override event EventHandler<TestRunStartEventArgs> TestRunStart;
        public override event EventHandler<TestResultEventArgs> TestResult;
        public override event EventHandler<TestRunCompleteEventArgs> TestRunComplete;
        public override event EventHandler<DiscoveryStartEventArgs> DiscoveryStart;
        public override event EventHandler<TestRunMessageEventArgs> DiscoveryMessage;
        public override event EventHandler<DiscoveredTestsEventArgs> DiscoveredTests;
        public override event EventHandler<DiscoveryCompleteEventArgs> DiscoveryComplete;
#pragma warning restore CS0067
    }

    private static Logger Create()
    {
        return new();
    }
}

public static class LoggerExtensions
{
    public static ILogger ConsoleLogger(
        this Logger logger
    )
    {
        return logger.Get<ILogger>("_logger");
    }
}