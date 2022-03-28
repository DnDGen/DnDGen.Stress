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
            options = new StressorOptions();
            options.RunningAssembly = Assembly.GetExecutingAssembly();
            options.ConfidenceIterations = 1_000;
            options.BuildTimeLimitInSeconds = 10;
            options.OutputTimeLimitInSeconds = 1;

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

            stopwatch.Start();
            Assert.That(() => stressor.GenerateOrFail(() => SlowGenerate(ref count), c => false), Throws.InstanceOf<AssertionException>().With.Message.EqualTo("Generation timed out"));
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
            Assert.That(output, Contains.Item($"Stress timeout is {stressor.TimeLimit}"));
        }

        [Test]
        public void WritesStressSummaryToConsole()
        {
            options.ConfidenceIterations = 10_000;

            var count = 0;

            var result = stressor.GenerateOrFail(() => count++, c => c == 9266);
            Assert.That(result, Is.EqualTo(9266));
            Assert.That(count, Is.EqualTo(9267));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(8));
            Assert.That(output[0], Is.EqualTo("Beginning stress test 'WritesStressSummaryToConsole'"));
            Assert.That(output[1], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[2], Is.EqualTo("Stress test 'WritesStressSummaryToConsole' complete"));
            Assert.That(output[3], Is.EqualTo("\tFull Name: DnDGen.Stress.Tests.GenerateOrFailTests.WritesStressSummaryToConsole"));
            Assert.That(output[4], Is.EqualTo($"\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"));
            Assert.That(output[5], Is.EqualTo($"\tCompleted Iterations: 9267 (92.67%)"));
            Assert.That(output[6], Is.EqualTo($"\tIterations Per Second: {stressor.TestIterations / stressor.TestDuration.TotalSeconds:N2}"));
            Assert.That(output[7], Is.EqualTo("\tLikely Status: PASSED"));
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

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(8));
            Assert.That(output[0], Is.EqualTo($"Beginning stress test 'WritesStressSummaryToConsole_WithParameters({caseNumber})'"));
            Assert.That(output[1], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[2], Is.EqualTo($"Stress test 'WritesStressSummaryToConsole_WithParameters({caseNumber})' complete"));
            Assert.That(output[3], Is.EqualTo($"\tFull Name: DnDGen.Stress.Tests.GenerateOrFailTests.WritesStressSummaryToConsole_WithParameters({caseNumber})"));
            Assert.That(output[4], Is.EqualTo($"\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"));
            Assert.That(output[5], Is.EqualTo("\tCompleted Iterations: 9267 (92.67%)"));
            Assert.That(output[6], Is.EqualTo($"\tIterations Per Second: {stressor.TestIterations / stressor.TestDuration.TotalSeconds:N2}"));
            Assert.That(output[7], Is.EqualTo("\tLikely Status: PASSED"));
        }

        [Test]
        public void WritesFailedStressSummaryToConsole()
        {
            var count = 0;
            Assert.That(() => stressor.GenerateOrFail(() => count++, c => false), Throws.InstanceOf<AssertionException>());

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(8));
            Assert.That(output[0], Is.EqualTo("Beginning stress test 'WritesFailedStressSummaryToConsole'"));
            Assert.That(output[1], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[2], Is.EqualTo("Stress test 'WritesFailedStressSummaryToConsole' complete"));
            Assert.That(output[3], Is.EqualTo("\tFull Name: DnDGen.Stress.Tests.GenerateOrFailTests.WritesFailedStressSummaryToConsole"));
            Assert.That(output[4], Is.EqualTo($"\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"));
            Assert.That(output[5], Is.EqualTo($"\tCompleted Iterations: {stressor.TestIterations} ({(double)stressor.TestIterations / options.ConfidenceIterations:P})"));
            Assert.That(output[6], Is.EqualTo($"\tIterations Per Second: {stressor.TestIterations / stressor.TestDuration.TotalSeconds:N2}"));
            Assert.That(output[7], Is.EqualTo("\tLikely Status: FAILED"));
        }

        [Test]
        public void WritesShortSummaryToConsole()
        {
            var count = 0;

            var result = stressor.GenerateOrFail(() => count++, c => c > 2);
            Assert.That(result, Is.EqualTo(3));
            Assert.That(count, Is.EqualTo(4));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(8));
            Assert.That(output[0], Is.EqualTo("Beginning stress test 'WritesShortSummaryToConsole'"));
            Assert.That(output[1], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[2], Is.EqualTo("Stress test 'WritesShortSummaryToConsole' complete"));
            Assert.That(output[3], Is.EqualTo("\tFull Name: DnDGen.Stress.Tests.GenerateOrFailTests.WritesShortSummaryToConsole"));
            Assert.That(output[4], Is.EqualTo($"\tTime: {stressor.TestDuration} ({stressor.TestDuration.TotalSeconds / stressor.TimeLimit.TotalSeconds:P})"));
            Assert.That(output[5], Is.EqualTo($"\tCompleted Iterations: {stressor.TestIterations} ({(double)stressor.TestIterations / options.ConfidenceIterations:P})"));
            Assert.That(output[6], Is.EqualTo($"\tIterations Per Second: {stressor.TestIterations / stressor.TestDuration.TotalSeconds:N2}"));
            Assert.That(output[7], Is.EqualTo("\tLikely Status: PASSED"));
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
