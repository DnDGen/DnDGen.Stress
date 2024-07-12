using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace DnDGen.Stress.Tests.Unit
{
    [TestFixture]
    public class GenerateTests
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
        public void DoesNotStopWhenTimeLimitHit()
        {
            var count = 0;

            stopwatch.Start();
            var result = stressor.Generate(() => count++, c => c == 9266 * 90210);
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.GreaterThan(stressor.TimeLimit));
            Assert.That(result, Is.EqualTo(9266 * 90210));
            Assert.That(count, Is.EqualTo(9266 * 90210 + 1));
        }

        [Test]
        public void DoesNotStopWhenIterationLimitHit()
        {
            var count = 0;

            stopwatch.Start();
            var result = stressor.Generate(() => count++, c => c == options.ConfidenceIterations + 1);
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit));
            Assert.That(result, Is.EqualTo(options.ConfidenceIterations + 1));
            Assert.That(count, Is.EqualTo(options.ConfidenceIterations + 2));
        }

        [Test]
        public void StopsWhenGenerated()
        {
            var count = 0;

            stopwatch.Start();
            var result = stressor.Generate(() => count++, c => c > 9266);
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit));
            Assert.That(result, Is.EqualTo(9267));
            Assert.That(count, Is.EqualTo(9268));
        }

        [Test]
        public void GenerateDoesNotWriteToConsole()
        {
            var count = 0;

            var result = stressor.Generate(() => count++, c => c > 9266);
            Assert.That(result, Is.EqualTo(9267));
            Assert.That(count, Is.EqualTo(9268));

            Assert.That(output, Is.Empty);
        }

        [Test]
        public void Generate()
        {
            var count = 0;
            var result = stressor.Generate(() => count++, c => c == 9266);
            Assert.That(result, Is.EqualTo(9266));
        }

        [Test]
        public void GenerateWithinGenerateHonorsTimeLimit()
        {
            var count = 0;

            stopwatch.Start();
            var result = stressor.Generate(() => stressor.Generate(() => count++, c => c % 9021 == 0), c => c == 90210);
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.LessThanOrEqualTo(stressor.TimeLimit));
            Assert.That(result, Is.EqualTo(90210));
            Assert.That(count, Is.EqualTo(90211));
        }

        [Test]
        public void PreserveStackTrace()
        {
            var count = 0;

            var exception = Assert.Throws<ArgumentException>(() => stressor.Generate(() => FailGeneration(count++), c => c > 9266));
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
