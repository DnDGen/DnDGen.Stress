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
    public class GenerateOrFailTests
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
            Assert.That(() => stressor.GenerateOrFail(() => SlowGenerate(ref count), c => false),
                Throws.InstanceOf<AssertionException>().With.Message.EqualTo("Generation timed out"));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TestDuration).Within(.1).Seconds);
            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.1).Seconds);
            Assert.That(stressor.TestIterations, Is.LessThan(options.ConfidenceIterations));
            Assert.That(count, Is.LessThan(options.ConfidenceIterations));
        }

        private int SlowGenerate(ref int count)
        {
            count++;
            Thread.Sleep(1);
            return count;
        }

        [Test]
        public void StopsWhenConfidenceIterationsHit()
        {
            var count = 0;

            stopwatch.Start();
            Assert.That(() => stressor.GenerateOrFail(() => count++, c => false), Throws.InstanceOf<AssertionException>().With.Message.EqualTo("Generation timed out"));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TestDuration).Within(.1).Seconds);
            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit));
            Assert.That(count, Is.EqualTo(options.ConfidenceIterations));
            Assert.That(stressor.TestIterations, Is.EqualTo(options.ConfidenceIterations));
        }

        [Test]
        public void StopsWhenGenerated()
        {
            options.ConfidenceIterations = 10_000;

            var count = 0;

            stopwatch.Start();
            var result = stressor.GenerateOrFail(() => count++, c => c > 9266);
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TestDuration).Within(.1).Seconds);
            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit));
            Assert.That(result, Is.EqualTo(9267));
            Assert.That(count, Is.EqualTo(9268));
            Assert.That(stressor.TestIterations, Is.EqualTo(9268));
        }

        [Test]
        public void WritesStressDurationToConsole()
        {
            options.ConfidenceIterations = 10_000;

            var count = 0;

            var result = stressor.GenerateOrFail(() => count++, c => c > 9266);
            Assert.That(result, Is.EqualTo(9267));
            Assert.That(count, Is.EqualTo(9268));

            Assert.That(output, Is.Not.Empty);
            Assert.That(output[0], Contains.Substring($"Stress timeout is {stressor.TimeLimit}"));
        }

        [Test]
        public void WritesStressSummaryToConsole()
        {
            options.ConfidenceIterations = 10_000;

            var count = 0;

            var result = stressor.GenerateOrFail(() => count++, c => c == 9266);
            Assert.That(result, Is.EqualTo(9266));
            Assert.That(count, Is.EqualTo(9267));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(2));
            Assert.That(output[0], Is.EqualTo("Beginning stress test 'WritesStressSummaryToConsole'"
                + $"{Environment.NewLine}Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo("Stress test 'WritesStressSummaryToConsole' complete"
                + $"{Environment.NewLine}\tFull Name: DnDGen.Stress.Tests.Unit.GenerateOrFailTests.WritesStressSummaryToConsole"
                + $"{Environment.NewLine}\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"
                + $"{Environment.NewLine}\tCompleted Iterations: 9,267 (92.67%)"
                + $"{Environment.NewLine}\tIterations Per Second: {stressor.TestIterations / stressor.TestDuration.TotalSeconds:N2}"
                + $"{Environment.NewLine}\tLikely Status: PASSED"));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void WritesStressSummaryToConsole_WithParameters(int caseNumber)
        {
            options.ConfidenceIterations = 10_000;

            var count = 0;

            var result = stressor.GenerateOrFail(() => count++, c => c == 9266);
            Assert.That(result, Is.EqualTo(9266));
            Assert.That(count, Is.EqualTo(9267));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(2));
            Assert.That(output[0], Is.EqualTo($"Beginning stress test 'WritesStressSummaryToConsole_WithParameters({caseNumber})'"
                + $"{Environment.NewLine}Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test 'WritesStressSummaryToConsole_WithParameters({caseNumber})' complete"
                + $"{Environment.NewLine}\tFull Name: DnDGen.Stress.Tests.Unit.GenerateOrFailTests.WritesStressSummaryToConsole_WithParameters({caseNumber})"
                + $"{Environment.NewLine}\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"
                + $"{Environment.NewLine}\tCompleted Iterations: 9,267 (92.67%)"
                + $"{Environment.NewLine}\tIterations Per Second: {stressor.TestIterations / stressor.TestDuration.TotalSeconds:N2}"
                + $"{Environment.NewLine}\tLikely Status: PASSED"));
        }

        [Test]
        public void WritesFailedStressSummaryToConsole()
        {
            var count = 0;
            Assert.That(() => stressor.GenerateOrFail(() => count++, c => false), Throws.InstanceOf<AssertionException>());

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(2));
            Assert.That(output[0], Is.EqualTo("Beginning stress test 'WritesFailedStressSummaryToConsole'"
                + $"{Environment.NewLine}Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo("Stress test 'WritesFailedStressSummaryToConsole' complete"
                + $"{Environment.NewLine}\tFull Name: DnDGen.Stress.Tests.Unit.GenerateOrFailTests.WritesFailedStressSummaryToConsole"
                + $"{Environment.NewLine}\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"
                + $"{Environment.NewLine}\tCompleted Iterations: {stressor.TestIterations:N0} ({(double)stressor.TestIterations / options.ConfidenceIterations:P})"
                + $"{Environment.NewLine}\tIterations Per Second: {stressor.TestIterations / stressor.TestDuration.TotalSeconds:N2}"
                + $"{Environment.NewLine}\tLikely Status: FAILED"));
        }

        [Test]
        public void WritesShortSummaryToConsole()
        {
            var count = 0;

            var result = stressor.GenerateOrFail(() => count++, c => c > 2);
            Assert.That(result, Is.EqualTo(3));
            Assert.That(count, Is.EqualTo(4));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(2));
            Assert.That(output[0], Is.EqualTo("Beginning stress test 'WritesShortSummaryToConsole'"
                + $"{Environment.NewLine}Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo("Stress test 'WritesShortSummaryToConsole' complete"
                + $"{Environment.NewLine}\tFull Name: DnDGen.Stress.Tests.Unit.GenerateOrFailTests.WritesShortSummaryToConsole"
                + $"{Environment.NewLine}\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"
                + $"{Environment.NewLine}\tCompleted Iterations: {stressor.TestIterations} ({(double)stressor.TestIterations / options.ConfidenceIterations:P})"
                + $"{Environment.NewLine}\tIterations Per Second: {stressor.TestIterations / stressor.TestDuration.TotalSeconds:N2}"
                + $"{Environment.NewLine}\tLikely Status: PASSED"));
        }

        [Test]
        public void GenerateAndSucceed()
        {
            options.ConfidenceIterations = 10_000;

            var count = 1;
            var result = stressor.GenerateOrFail(() => count += count, c => c >= 9266);

            Assert.That(stressor.TestDuration, Is.LessThan(stressor.TimeLimit));
            Assert.That(stressor.TestIterations, Is.EqualTo(14));
            Assert.That(result, Is.EqualTo(16384));
        }

        [Test]
        public void GenerateAndFail()
        {
            var count = 0;
            Assert.That(() => stressor.GenerateOrFail(() => count++, c => false), Throws.InstanceOf<AssertionException>().With.Message.EqualTo("Generation timed out"));
        }

        [Test]
        public void GenerateWithinGenerateOrFailHonorsTimeLimit()
        {
            options.ConfidenceIterations = 10_000_000;

            var count = 0;

            stopwatch.Start();
            Assert.That(
                () => stressor.GenerateOrFail(
                    () => stressor.Generate(
                        () => count++,
                        c => c % 9266 == 0),
                    c => c < 0),
                Throws.InstanceOf<AssertionException>());
            stopwatch.Stop();

            Assert.That(stressor.TestDuration, Is.EqualTo(stressor.TimeLimit).Within(.1).Seconds);
            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.1).Seconds);
            Assert.That(stressor.TestIterations, Is.AtLeast(1000));
            Assert.That(count, Is.AtLeast(9266 * (stressor.TestIterations - 1)));
        }

        [Test]
        public void PreserveStackTrace()
        {
            var count = 0;

            var exception = Assert.Throws<ArgumentException>(() => stressor.GenerateOrFail(() => FailGeneration(count++), c => c > 9266));
            Assert.That(count, Is.EqualTo(12));
            Assert.That(exception.StackTrace, Contains.Substring("FailGeneration"));
        }

        public int FailGeneration(int count)
        {
            if (count > 10)
                throw new ArgumentException();

            return count;
        }
    }
}
