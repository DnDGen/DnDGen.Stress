using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace DnDGen.Stress.Tests.Unit
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
            options = new StressorOptions
            {
                RunningAssembly = Assembly.GetExecutingAssembly(),
                ConfidenceIterations = 1_000,
                BuildTimeLimitInSeconds = 10,
                OutputTimeLimitInSeconds = 1
            };

            output = [];
            mockLogger = new Mock<ILogger>();
            mockLogger
                .Setup(l => l.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => AddLog(v.ToString())),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)));

            stressor = new Stressor(options, mockLogger.Object);
            stopwatch = new Stopwatch();
        }

        private bool AddLog(string message)
        {
            output.Add(message);
            return true;
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

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.1).Seconds);
            Assert.That(stressor.TestDuration, Is.EqualTo(stressor.TimeLimit).Within(.1).Seconds);
            Assert.That(stressor.TestIterations, Is.LessThan(options.ConfidenceIterations));
            Assert.That(count, Is.LessThan(options.ConfidenceIterations));
            Assert.That(setup, Is.LessThan(options.ConfidenceIterations));
            Assert.That(teardown, Is.LessThan(options.ConfidenceIterations));
            Assert.That(count, Is.EqualTo(setup)
                .And.EqualTo(teardown));
        }

        private void SlowTest(ref int count)
        {
            FastTest(ref count);
            Thread.Sleep(1);
        }

        private void FastTest(ref int count, int failLimit = int.MaxValue)
        {
            count++;
            Assert.That(count, Is.Positive.And.LessThan(failLimit));
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

            Assert.That(stressor.TestDuration, Is.EqualTo(stopwatch.Elapsed).Within(.1).Seconds
                .And.LessThan(stressor.TimeLimit));
            Assert.That(stressor.TestIterations, Is.Positive.And.EqualTo(options.ConfidenceIterations));
            Assert.That(count, Is.EqualTo(options.ConfidenceIterations));
            Assert.That(setup, Is.EqualTo(options.ConfidenceIterations));
            Assert.That(teardown, Is.EqualTo(options.ConfidenceIterations));
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
            Assert.That(output[0], Contains.Substring($"Stress timeout is {stressor.TimeLimit}"));
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

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(2));
            Assert.That(output[0], Is.EqualTo("Beginning stress test 'WritesStressSummaryToConsole'"
                + $"{Environment.NewLine}Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo("Stress test 'WritesStressSummaryToConsole' complete"
                + $"{Environment.NewLine}\tFull Name: DnDGen.Stress.Tests.Unit.StressWithSetupAndTeardownTests.WritesStressSummaryToConsole"
                + $"{Environment.NewLine}\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"
                + $"{Environment.NewLine}\tCompleted Iterations: {stressor.TestIterations:N0} ({(double)stressor.TestIterations / options.ConfidenceIterations:P})"
                + $"{Environment.NewLine}\tIterations Per Second: {stressor.TestIterations / stressor.TestDuration.TotalSeconds:N2}"
                + $"{Environment.NewLine}\tLikely Status: PASSED"));

            Assert.That(stressor.TestIterations, Is.EqualTo(count)
                .And.EqualTo(setup)
                .And.EqualTo(teardown)
                .And.EqualTo(1000));
            Assert.That(output[1], Contains.Substring($"Completed Iterations: 1,000"));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void WritesStressSummaryToConsole_WithParameters(int caseNumber)
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(
                () => TestSetup(ref setup),
                () => FastTest(ref count),
                () => TestTeardown(ref teardown));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(2));
            Assert.That(output[0], Is.EqualTo($"Beginning stress test 'WritesStressSummaryToConsole_WithParameters({caseNumber})'"
                + $"{Environment.NewLine}Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test 'WritesStressSummaryToConsole_WithParameters({caseNumber})' complete"
                + $"{Environment.NewLine}\tFull Name: DnDGen.Stress.Tests.Unit.StressWithSetupAndTeardownTests.WritesStressSummaryToConsole_WithParameters({caseNumber})"
                + $"{Environment.NewLine}\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"
                + $"{Environment.NewLine}\tCompleted Iterations: {stressor.TestIterations:N0} ({(double)stressor.TestIterations / options.ConfidenceIterations:P})"
                + $"{Environment.NewLine}\tIterations Per Second: {stressor.TestIterations / stressor.TestDuration.TotalSeconds:N2}"
                + $"{Environment.NewLine}\tLikely Status: PASSED"));
        }

        [Test]
        public void WritesFailedStressSummaryToConsole()
        {
            var setup = 0;
            var teardown = 0;

            Assert.That(() => stressor.Stress(() => setup++, FailedTest, () => teardown++), Throws.InstanceOf<AssertionException>());
            Assert.That(setup, Is.EqualTo(1));
            Assert.That(teardown, Is.EqualTo(1));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(2));
            Assert.That(output[0], Is.EqualTo("Beginning stress test 'WritesFailedStressSummaryToConsole'"
                + $"{Environment.NewLine}Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo("Stress test 'WritesFailedStressSummaryToConsole' complete"
                + $"{Environment.NewLine}\tFull Name: DnDGen.Stress.Tests.Unit.StressWithSetupAndTeardownTests.WritesFailedStressSummaryToConsole"
                + $"{Environment.NewLine}\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"
                + $"{Environment.NewLine}\tCompleted Iterations: 0 (0.00%)"
                + $"{Environment.NewLine}\tIterations Per Second: 0.00"
                + $"{Environment.NewLine}\tLikely Status: FAILED"));
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

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(2));
            Assert.That(output[0], Is.EqualTo("Beginning stress test 'WritesStressSlowSummaryToConsole'"
                + $"{Environment.NewLine}Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo("Stress test 'WritesStressSlowSummaryToConsole' complete"
                + $"{Environment.NewLine}\tFull Name: DnDGen.Stress.Tests.Unit.StressWithSetupAndTeardownTests.WritesStressSlowSummaryToConsole"
                + $"{Environment.NewLine}\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"
                + $"{Environment.NewLine}\tCompleted Iterations: {stressor.TestIterations} ({(double)stressor.TestIterations / options.ConfidenceIterations:P})"
                + $"{Environment.NewLine}\tIterations Per Second: {stressor.TestIterations / stressor.TestDuration.TotalSeconds:N2}"
                + $"{Environment.NewLine}\tLikely Status: PASSED"));
        }

        [Test]
        public void StressATestWithSetupAndTeardown_HitIterations()
        {
            options.IsFullStress = true;
            options.ConfidenceIterations = 1_000;
            options.BuildTimeLimitInSeconds = 1_000;
            options.OutputTimeLimitInSeconds = 1_000;

            var count = 0;
            var setup = 0;
            var teardown = 0;
            stressor.Stress(() => TestSetup(ref setup), () => FastTest(ref count), () => TestTeardown(ref teardown));

            Assert.That(stressor.TestDuration, Is.LessThan(stressor.TimeLimit));
            Assert.That(stressor.TestIterations, Is.AtLeast(options.ConfidenceIterations));
            Assert.That(count, Is.AtLeast(options.ConfidenceIterations), "Count");
            Assert.That(setup, Is.AtLeast(options.ConfidenceIterations), "Setup");
            Assert.That(teardown, Is.AtLeast(options.ConfidenceIterations), "Tear Down");
            Assert.That(count, Is.EqualTo(stressor.TestIterations)
                .And.EqualTo(setup)
                .And.EqualTo(teardown));
        }

        [Test]
        public void StressATestWithSetupAndTeardown_HitTimeLimit_Output()
        {
            options.IsFullStress = true;
            options.ConfidenceIterations = 10_000_000;
            options.BuildTimeLimitInSeconds = 1_000;
            options.OutputTimeLimitInSeconds = 1;

            stressor = new Stressor(options, mockLogger.Object);

            var count = 0;
            var setup = 0;
            var teardown = 0;
            stressor.Stress(() => TestSetup(ref setup), () => FastTest(ref count), () => TestTeardown(ref teardown));

            Assert.That(stressor.TimeLimit, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(stressor.TestDuration, Is.AtLeast(stressor.TimeLimit));
            Assert.That(stressor.TestIterations, Is.LessThan(options.ConfidenceIterations));
            Assert.That(count, Is.LessThan(options.ConfidenceIterations), "Count");
            Assert.That(setup, Is.LessThan(options.ConfidenceIterations), "Setup");
            Assert.That(teardown, Is.LessThan(options.ConfidenceIterations), "Tear Down");
            Assert.That(count, Is.EqualTo(stressor.TestIterations)
                .And.EqualTo(setup)
                .And.EqualTo(teardown));
        }

        [Test]
        public void StressATestWithSetupAndTeardown_HitTimeLimit_Build()
        {
            options.IsFullStress = true;
            options.ConfidenceIterations = 10_000_000;
            options.BuildTimeLimitInSeconds = 3 * (StressorTests.TestCaseCount + StressorTests.TestCount);
            options.OutputTimeLimitInSeconds = 10;

            stressor = new Stressor(options, mockLogger.Object);

            var count = 0;
            var setup = 0;
            var teardown = 0;
            stressor.Stress(() => TestSetup(ref setup), () => FastTest(ref count), () => TestTeardown(ref teardown));

            Assert.That(stressor.TimeLimit, Is.EqualTo(TimeSpan.FromSeconds(3)).Within(0.1).Seconds);
            Assert.That(stressor.TestDuration, Is.AtLeast(stressor.TimeLimit));
            Assert.That(stressor.TestIterations, Is.LessThan(options.ConfidenceIterations));
            Assert.That(count, Is.LessThan(options.ConfidenceIterations), "Count");
            Assert.That(setup, Is.LessThan(options.ConfidenceIterations), "Setup");
            Assert.That(teardown, Is.LessThan(options.ConfidenceIterations), "Tear Down");
            Assert.That(count, Is.EqualTo(stressor.TestIterations)
                .And.EqualTo(setup)
                .And.EqualTo(teardown));
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
            options.ConfidenceIterations = 10_000;

            var count = 0;
            var setup = 0;
            var teardown = 0;

            Assert.That(() => stressor.Stress(() => TestSetup(ref setup), () => FastTest(ref count, 9266), () => TestTeardown(ref teardown)),
                Throws.InstanceOf<AssertionException>());
            Assert.That(stressor.TestDuration, Is.LessThan(stressor.TimeLimit));
            Assert.That(stressor.TestIterations, Is.EqualTo(9265));
            Assert.That(count, Is.EqualTo(9266), "Count");
            Assert.That(setup, Is.EqualTo(9266), "Setup");
            Assert.That(teardown, Is.EqualTo(9266), "Tear Down");
        }

        [Test]
        public void GenerateWithinStressWithSetupAndTeardownHonorsTimeLimit()
        {
            options.ConfidenceIterations = 10_000_000;

            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(
                () => TestSetup(ref setup),
                () => TestWithGenerate(ref count),
                () => TestTeardown(ref teardown));

            Assert.That(stressor.TestDuration, Is.EqualTo(stressor.TimeLimit).Within(.1).Seconds);
            Assert.That(count, Is.LessThan(options.ConfidenceIterations)
                .And.AtLeast(1000), "Count");
            Assert.That(setup, Is.LessThan(options.ConfidenceIterations)
                .And.AtLeast(1000), "Setup");
            Assert.That(teardown, Is.LessThan(options.ConfidenceIterations)
                .And.AtLeast(1000), "Tear Down");
        }

        private void TestWithGenerate(ref int count)
        {
            count++;
            var subcount = 0;

            var result = stressor.Generate(() => subcount++, c => c == 9266);
            Assert.That(result, Is.EqualTo(9266));
            Assert.That(count, Is.Positive);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(10)]
        [TestCase(65)]
        [TestCase(70)]
        [TestCase(100)]
        [TestCase(1000)]
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

            Assert.That(stressor.TestDuration, Is.AtLeast(stressor.TimeLimit));
            Assert.That(stressor.TestIterations, Is.LessThan(options.ConfidenceIterations));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(2));
            Assert.That(output[1], Contains.Substring($"\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"));
            Assert.That(output[1], Contains.Substring($"\tCompleted Iterations: {stressor.TestIterations} ({(double)stressor.TestIterations / options.ConfidenceIterations:P})"));
            Assert.That(output[1], Contains.Substring($"\tIterations Per Second: {stressor.TestIterations / stressor.TestDuration.TotalSeconds:N2}"));
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
            Assert.That(exception.StackTrace.Trim(), Does.Not.StartsWith("at DnDGen.Stress.Stressor.RunAction"));
        }

        public void FailStress(int count)
        {
            if (count > 10)
                throw new ArgumentException();
        }
    }
}
