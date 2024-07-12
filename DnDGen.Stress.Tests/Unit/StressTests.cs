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
    public class StressTests
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

            stopwatch.Start();
            stressor.Stress(() => SlowTest(ref count));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.1).Seconds);
            Assert.That(stressor.TestDuration, Is.EqualTo(stressor.TimeLimit).Within(.1).Seconds);
            Assert.That(stressor.TestIterations, Is.LessThan(options.ConfidenceIterations));
            Assert.That(count, Is.LessThan(options.ConfidenceIterations));
        }

        private void SlowTest(ref int count)
        {
            FastTest(ref count);
            Thread.Sleep(1);
        }

        private void FastTest(ref int count)
        {
            count++;
            Assert.That(count, Is.Positive);
        }

        [Test]
        public void StopsWhenConfidenceIterationsHit()
        {
            options.IsFullStress = true;
            options.TestCount = 1;
            options.RunningAssembly = null;

            stressor = new Stressor(options, mockLogger.Object);

            var count = 0;

            stopwatch.Start();
            stressor.Stress(() => FastTest(ref count));
            stopwatch.Stop();

            Assert.That(stressor.TestDuration, Is.EqualTo(stopwatch.Elapsed).Within(.1).Seconds
                .And.LessThan(stressor.TimeLimit));
            Assert.That(stressor.TestIterations, Is.Positive.And.EqualTo(options.ConfidenceIterations));
            Assert.That(count, Is.EqualTo(options.ConfidenceIterations));
        }

        [Test]
        public void WritesStressDurationToConsole()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));

            Assert.That(output, Is.Not.Empty);
            Assert.That(output[0], Contains.Substring($"Stress timeout is {stressor.TimeLimit}"));
        }

        [Test]
        public void WritesStressSummaryToConsole()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(2));
            Assert.That(output[0], Is.EqualTo("Beginning stress test 'WritesStressSummaryToConsole'"
                + $"{Environment.NewLine}Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo("Stress test 'WritesStressSummaryToConsole' complete"
                + $"{Environment.NewLine}\tFull Name: DnDGen.Stress.Tests.Unit.StressTests.WritesStressSummaryToConsole"
                + $"{Environment.NewLine}\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"
                + $"{Environment.NewLine}\tCompleted Iterations: {stressor.TestIterations} ({(double)stressor.TestIterations / options.ConfidenceIterations:P})"
                + $"{Environment.NewLine}\tIterations Per Second: {stressor.TestIterations / stressor.TestDuration.TotalSeconds:N2}"
                + $"{Environment.NewLine}\tLikely Status: PASSED"));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void WritesStressSummaryToConsole_WithParameters(int caseNumber)
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(2));
            Assert.That(output[0], Is.EqualTo($"Beginning stress test 'WritesStressSummaryToConsole_WithParameters({caseNumber})'"
                + $"{Environment.NewLine}Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test 'WritesStressSummaryToConsole_WithParameters({caseNumber})' complete"
                + $"{Environment.NewLine}\tFull Name: DnDGen.Stress.Tests.Unit.StressTests.WritesStressSummaryToConsole_WithParameters({caseNumber})"
                + $"{Environment.NewLine}\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"
                + $"{Environment.NewLine}\tCompleted Iterations: {stressor.TestIterations} ({(double)stressor.TestIterations / options.ConfidenceIterations:P})"
                + $"{Environment.NewLine}\tIterations Per Second: {stressor.TestIterations / stressor.TestDuration.TotalSeconds:N2}"
                + $"{Environment.NewLine}\tLikely Status: PASSED"));
        }

        [Test]
        public void WritesFailedStressSummaryToConsole()
        {
            Assert.That(() => stressor.Stress(FailedTest), Throws.InstanceOf<AssertionException>());

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(2));
            Assert.That(output[0], Is.EqualTo("Beginning stress test 'WritesFailedStressSummaryToConsole'"
                + $"{Environment.NewLine}Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo("Stress test 'WritesFailedStressSummaryToConsole' complete"
                + $"{Environment.NewLine}\tFull Name: DnDGen.Stress.Tests.Unit.StressTests.WritesFailedStressSummaryToConsole"
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
            stressor.Stress(() => SlowTest(ref count));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(2));
            Assert.That(output[0], Is.EqualTo("Beginning stress test 'WritesStressSlowSummaryToConsole'"
                + $"{Environment.NewLine}Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo("Stress test 'WritesStressSlowSummaryToConsole' complete"
                + $"{Environment.NewLine}\tFull Name: DnDGen.Stress.Tests.Unit.StressTests.WritesStressSlowSummaryToConsole"
                + $"{Environment.NewLine}\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"
                + $"{Environment.NewLine}\tCompleted Iterations: {stressor.TestIterations} ({(double)stressor.TestIterations / options.ConfidenceIterations:P})"
                + $"{Environment.NewLine}\tIterations Per Second: {stressor.TestIterations / stressor.TestDuration.TotalSeconds:N2}"
                + $"{Environment.NewLine}\tLikely Status: PASSED"));
        }

        [Test]
        public void StressATest_HitIterations()
        {
            options.IsFullStress = true;
            options.ConfidenceIterations = 1_000;
            options.BuildTimeLimitInSeconds = 1_000;
            options.OutputTimeLimitInSeconds = 1_000;

            var count = 0;
            stressor.Stress(() => FastTest(ref count));

            Assert.That(stressor.TestDuration, Is.LessThan(stressor.TimeLimit));
            Assert.That(stressor.TestIterations, Is.AtLeast(options.ConfidenceIterations));
            Assert.That(count, Is.AtLeast(options.ConfidenceIterations));
        }

        [Test]
        public void StressATest_HitTimeLimit_Output()
        {
            options.IsFullStress = true;
            options.ConfidenceIterations = int.MaxValue;
            options.BuildTimeLimitInSeconds = 1_000;
            options.OutputTimeLimitInSeconds = 1;

            stressor = new Stressor(options, mockLogger.Object);

            var count = 0;
            stressor.Stress(() => FastTest(ref count));

            Assert.That(stressor.TimeLimit, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(stressor.TestIterations, Is.LessThan(options.ConfidenceIterations));
            Assert.That(stressor.TestDuration, Is.AtLeast(stressor.TimeLimit));
            Assert.That(count, Is.LessThan(options.ConfidenceIterations));
        }

        [Test]
        public void StressATest_HitTimeLimit_Build()
        {
            options.IsFullStress = true;
            options.ConfidenceIterations = int.MaxValue;
            options.BuildTimeLimitInSeconds = 3 * (StressorTests.TestCaseCount + StressorTests.TestCount);
            options.OutputTimeLimitInSeconds = 10;

            stressor = new Stressor(options, mockLogger.Object);

            var count = 0;
            stressor.Stress(() => FastTest(ref count));

            Assert.That(stressor.TimeLimit, Is.EqualTo(TimeSpan.FromSeconds(3)).Within(0.1).Seconds);
            Assert.That(stressor.TestIterations, Is.LessThan(options.ConfidenceIterations));
            Assert.That(stressor.TestDuration, Is.AtLeast(stressor.TimeLimit));
            Assert.That(count, Is.LessThan(options.ConfidenceIterations));
        }

        [Test]
        public void GenerateWithinStressHonorsTimeLimit()
        {
            options.ConfidenceIterations = int.MaxValue;

            var count = 0;
            stressor.Stress(() => TestWithGenerate(ref count));

            Assert.That(stressor.TestIterations, Is.LessThan(options.ConfidenceIterations));
            Assert.That(stressor.TestDuration, Is.EqualTo(stressor.TimeLimit).Within(.1).Seconds);
            Assert.That(count, Is.LessThan(options.ConfidenceIterations));
        }

        private void TestWithGenerate(ref int count)
        {
            var innerCount = count;
            count = stressor.Generate(() => innerCount++, c => c > 1);
            Assert.That(count, Is.AtLeast(2));
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
            stressor.Stress(() => SlowTest(ref count));

            Assert.That(stressor.TestDuration, Is.AtLeast(stressor.TimeLimit));
            Assert.That(stressor.TestIterations, Is.LessThan(options.ConfidenceIterations));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(2));
            Assert.That(output[1], Contains.Substring($"\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"));
            Assert.That(output[1],
                Contains.Substring($"\tCompleted Iterations: {stressor.TestIterations} ({(double)stressor.TestIterations / options.ConfidenceIterations:P})"));
            Assert.That(output[1], Contains.Substring($"\tIterations Per Second: {stressor.TestIterations / stressor.TestDuration.TotalSeconds:N2}"));
        }

        [Test]
        public void PreserveStackTrace()
        {
            var count = 0;

            var exception = Assert.Throws<ArgumentException>(() => stressor.Stress(() => FailStress(count++)));
            Assert.That(count, Is.EqualTo(12));
            Assert.That(exception.StackTrace.Trim(), Does.Not.StartsWith("at DnDGen.Stress.Stressor.RunAction"));
        }

        public void FailStress(int count)
        {
            if (count > 10)
                throw new ArgumentException();
        }
    }
}
