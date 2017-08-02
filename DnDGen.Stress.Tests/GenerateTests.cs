using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace DnDGen.Stress.Tests
{
    [TestFixture]
    public class GenerateTests
    {
        private Stressor stressor;
        private Assembly runningAssembly;
        private Stopwatch stopwatch;
        private StringBuilder console;

        [SetUp]
        public void Setup()
        {
            runningAssembly = Assembly.GetExecutingAssembly();
            stressor = new Stressor(false, runningAssembly);
            stopwatch = new Stopwatch();
            console = new StringBuilder();
            var writer = new StringWriter(console);

            Console.SetOut(writer);
        }

        [TearDown]
        public void Teardown()
        {
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);
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
            var result = stressor.Generate(() => count++, c => c == Stressor.ConfidentIterations + 1);
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit));
            Assert.That(result, Is.EqualTo(Stressor.ConfidentIterations + 1));
            Assert.That(count, Is.EqualTo(Stressor.ConfidentIterations + 2));
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

            var output = console.ToString();
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
    }
}
