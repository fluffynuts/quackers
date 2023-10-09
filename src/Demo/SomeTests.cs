using System;
using System.Collections.Generic;
using System.Threading;
using NExpect;
using NUnit.Framework;
using static NExpect.Expectations;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace QuackersTestHost
{
    [TestFixture]
    public class SomeTests
    {
        [Test]
        public void ShouldPass()
        {
            if (ForcePass)
            {
                Assert.Pass();
                return;
            }

            // Arrange
            // Act
            Expect(true)
                .To.Be.True();
            // Assert
        }

        [Test]
        public void ShouldFail()
        {
            if (ForcePass)
            {
                Assert.Pass();
                return;
            }

            // Arrange
            // Act
            Expect(true)
                .To.Be.False("this test should fail");
            // Assert
        }

        public static IEnumerable<int> SleepGenerator()
        {
            yield return 1;
            yield return 100;
            yield return 500;
            yield return 1500;
            yield return 1234;
        }

        [TestCaseSource(nameof(SleepGenerator))]
        public void LongerPasses(int sleepMs)
        {
            if (ForcePass)
            {
                Assert.Pass();
                return;
            }

            // Arrange
            Thread.Sleep(sleepMs);
            // Act
            Expect(true)
                .To.Be.True();
            // Assert
        }

        public static IEnumerable<int> MakeSomeNumbers()
        {
            for (var i = 0; i < 3; i++)
            {
                yield return GetRandomInt(1, 100);
            }
        }

        [TestCaseSource(nameof(MakeSomeNumbers))]
        public void ShouldBeLessThan50(int value)
        {
            if (ForcePass)
            {
                Assert.Pass();
                return;
            }

            // Arrange
            // Act
            Expect(value)
                .To.Be.Less.Than(50);
            // Assert
        }

        [Test]
        [Ignore("skipped because...")]
        public void SkippyTesty()
        {
            if (ForcePass)
            {
                Assert.Pass();
                return;
            }

            // Arrange
            // Act
            Expect(1)
                .To.Equal(2);
            // Assert
        }

        [Test]
        [Explicit("integration test")]
        public void ExplicitTest()
        {
            if (ForcePass)
            {
                Assert.Pass();
                return;
            }

            // Arrange
            // Act
            Expect(1)
                .To.Equal(2);
            // Assert
        }

        private bool ForcePass =>
            _forcePass ??= PassIsForcedViaEnvironment();

        private bool PassIsForcedViaEnvironment()
        {
            var envVar = Environment.GetEnvironmentVariable("FORCE_PASS") ?? "";
            return Truthy.Contains(envVar);
        }

        private static HashSet<string> Truthy = new(
            new[]
            {
                "true",
                "1",
                "yes"
            },
            StringComparer.OrdinalIgnoreCase
        );

        private bool? _forcePass;
    }
}