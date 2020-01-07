using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace DnDGen.Stress.Tests
{
    [TestFixture]
    public class StressWithSetupAndTeardownTests
    {
        private Stressor stressor;
        private Mock<ILogger> mockLogger;
        private List<string> output;
        private StressorOptions options;
        private Stopwatch stopwatch;

        [SetUp]
        public void Setup()
        {
            options = new StressorOptions();
            options.RunningAssembly = Assembly.GetExecutingAssembly();

            output = new List<string>();
            mockLogger = new Mock<ILogger>();
            mockLogger
                .Setup(l => l.Log(It.IsAny<string>()))
                .Callback((string m) => output.Add(m));

            stressor = new Stressor(options, mockLogger.Object);
            stopwatch = new Stopwatch();
        }

        [Test]
        public void StopsWhenTimeLimitHit()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stopwatch.Start();

            stressor.Stress(
                () => TestSetup(ref setup),
                () => SlowTest(ref count),
                () => TestTeardown(ref teardown));

            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.01).Seconds);
            Assert.That(count, Is.LessThan(Stressor.ConfidentIterations));
            Assert.That(setup, Is.LessThan(Stressor.ConfidentIterations));
            Assert.That(teardown, Is.LessThan(Stressor.ConfidentIterations));
            Assert.That(count, Is.EqualTo(setup));
            Assert.That(count, Is.EqualTo(teardown));
        }

        private void SlowTest(ref int count)
        {
            FastTest(ref count);
            Thread.Sleep(1);
        }

        private void FastTest(ref int count, int failLimit = int.MaxValue)
        {
            count++;
            Assert.That(count, Is.Positive);
            Assert.That(count, Is.LessThan(failLimit));
        }

        [Test]
        public void StopsWhenConfidenceIterationsHit()
        {
            options.IsFullStress = true;
            options.TestCount = 1;
            options.RunningAssembly = null;

            stressor = new Stressor(options, mockLogger.Object);

            var count = 0;
            var setup = 0;
            var teardown = 0;

            stopwatch.Start();

            stressor.Stress(
                () => TestSetup(ref setup),
                () => FastTest(ref count),
                () => TestTeardown(ref teardown));

            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit));
            Assert.That(count, Is.EqualTo(Stressor.ConfidentIterations));
            Assert.That(setup, Is.EqualTo(Stressor.ConfidentIterations));
            Assert.That(teardown, Is.EqualTo(Stressor.ConfidentIterations));
        }

        [Test]
        public void WritesStressDurationToConsole()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(
                () => TestSetup(ref setup),
                () => FastTest(ref count),
                () => TestTeardown(ref teardown));

            Assert.That(output, Is.Not.Empty);
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
        }

        [Test]
        public void WritesStressSummaryToConsole()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(
                () => TestSetup(ref setup),
                () => FastTest(ref count),
                () => TestTeardown(ref teardown));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(6));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:0"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public void WritesFailedStressSummaryToConsole()
        {
            var setup = 0;
            var teardown = 0;

            Assert.That(() => stressor.Stress(() => setup++, FailedTest, () => teardown++), Throws.InstanceOf<AssertionException>());
            Assert.That(setup, Is.EqualTo(1));
            Assert.That(teardown, Is.EqualTo(1));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(6));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(output[3], Is.EqualTo($"\tCompleted Iterations: 0 (0%)"));
            Assert.That(output[4], Is.EqualTo($"\tIterations Per Second: 0"));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: FAILED"));
        }

        private void FailedTest()
        {
            Assert.Fail("This test should fail");
        }

        [Test]
        public void WritesStressSlowSummaryToConsole()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(
                () => TestSetup(ref setup),
                () => SlowTest(ref count),
                () => TestTeardown(ref teardown));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(6));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public void StressATestWithSetupAndTeardown()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(() => TestSetup(ref setup), () => FastTest(ref count), () => TestTeardown(ref teardown));
            Assert.That(count, Is.AtLeast(10000), "Count");
            Assert.That(setup, Is.AtLeast(10000), "Setup");
            Assert.That(teardown, Is.AtLeast(10000), "Tear Down");
        }

        private void TestSetup(ref int setup)
        {
            setup++;
        }

        private void TestTeardown(ref int teardown)
        {
            teardown++;
        }

        [Test]
        public void StressAFailedTestWithSetupAndTeardown()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            Assert.That(() => stressor.Stress(() => TestSetup(ref setup), () => FastTest(ref count, 9266), () => TestTeardown(ref teardown)), Throws.InstanceOf<AssertionException>());
            Assert.That(count, Is.EqualTo(9266), "Count");
            Assert.That(setup, Is.EqualTo(9266), "Setup");
            Assert.That(teardown, Is.EqualTo(9266), "Tear Down");
        }

        [Test]
        public void GenerateWithinStressWithSetupAndTeardownHonorsTimeLimit()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stopwatch.Start();

            stressor.Stress(
                () => TestSetup(ref setup),
                () => TestWithGenerate(ref count),
                () => TestTeardown(ref teardown));

            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.01).Seconds);
            Assert.That(count, Is.LessThan(Stressor.ConfidentIterations), "Count");
            Assert.That(setup, Is.LessThan(Stressor.ConfidentIterations), "Setup");
            Assert.That(teardown, Is.LessThan(Stressor.ConfidentIterations), "Tear Down");
            Assert.That(count, Is.AtLeast(1000), "Count");
            Assert.That(setup, Is.AtLeast(1000), "Setup");
            Assert.That(teardown, Is.AtLeast(1000), "Tear Down");
        }

        private void TestWithGenerate(ref int count)
        {
            count++;
            var subcount = 0;

            var result = stressor.Generate(() => subcount++, c => c == 9266);
            Assert.That(result, Is.EqualTo(9266));
            Assert.That(count, Is.Positive);
        }

        [TestCase(65)]
        [TestCase(70)]
        public void PercentageIsAccurate(int testCount)
        {
            options.IsFullStress = true;
            options.TestCount = testCount;
            options.RunningAssembly = null;

            stressor = new Stressor(options, mockLogger.Object);

            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(
                () => TestSetup(ref setup),
                () => SlowTest(ref count),
                () => TestTeardown(ref teardown));

            var time = stressor.TimeLimit.ToString().Substring(0, 10);

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(6));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: {time}"));
            Assert.That(output[2], Does.Contain($"(100"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public void PreserveStackTrace()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            var exception = Assert.Throws<ArgumentException>(() => stressor.Stress(() => TestSetup(ref setup), () => FailStress(count++), () => TestTeardown(ref teardown)));
            Assert.That(count, Is.EqualTo(12));
            Assert.That(setup, Is.EqualTo(12));
            Assert.That(teardown, Is.EqualTo(12));
            Assert.That(exception.StackTrace.Trim(), Does.Not.StartsWith("at DnDGen.Stress.Stressor.RunAction(Action setup, Action action, Action teardown)"));
        }

        public void FailStress(int count)
        {
            if (count > 10)
                throw new ArgumentException();
        }
    }
}
