using Moq;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace DnDGen.Stress.Tests
{
    [TestFixture]
    public class StressWithSetupAndTeardownAsyncTests
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
        public async Task StopsWhenTimeLimitHit()
        {
            var counts = new BlockingCollection<bool>();
            var setups = new BlockingCollection<bool>();
            var teardowns = new BlockingCollection<bool>();

            stopwatch.Start();

            await stressor.StressAsync(
                () => TestSetup(setups),
                async () => await SlowTestAsync(counts),
                () => TestTeardown(teardowns));

            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.1).Seconds);
            Assert.That(counts, Has.Count.LessThan(Stressor.ConfidentIterations));
            Assert.That(setups, Has.Count.LessThan(Stressor.ConfidentIterations));
            Assert.That(teardowns, Has.Count.LessThan(Stressor.ConfidentIterations));
            Assert.That(counts, Has.Count.EqualTo(setups.Count));
            Assert.That(counts, Has.Count.EqualTo(teardowns.Count));
        }

        private async Task SlowTestAsync(BlockingCollection<bool> collection)
        {
            await FastTestAsync(collection);
            await Task.Delay(1);
        }

        private async Task FastTestAsync(BlockingCollection<bool> collection, int failLimit = int.MaxValue)
        {
            collection.Add(true);
            Assert.That(collection, Has.Count.Positive
                .And.Count.LessThan(failLimit));
        }

        [Test]
        public async Task StopsWhenConfidenceIterationsHit()
        {
            options.IsFullStress = true;
            options.TestCount = 1;
            options.RunningAssembly = null;

            stressor = new Stressor(options, mockLogger.Object);

            var counts = new BlockingCollection<bool>();
            var setups = new BlockingCollection<bool>();
            var teardowns = new BlockingCollection<bool>();

            stopwatch.Start();

            await stressor.StressAsync(
                () => TestSetup(setups),
                async () => await FastTestAsync(counts),
                () => TestTeardown(teardowns));

            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit));
            Assert.That(counts, Has.Count.EqualTo(Stressor.ConfidentIterations));
            Assert.That(setups, Has.Count.EqualTo(Stressor.ConfidentIterations));
            Assert.That(teardowns, Has.Count.EqualTo(Stressor.ConfidentIterations));
        }

        [Test]
        public async Task WritesStressDurationToConsole()
        {
            var counts = new BlockingCollection<bool>();
            var setups = new BlockingCollection<bool>();
            var teardowns = new BlockingCollection<bool>();

            await stressor.StressAsync(
                () => TestSetup(setups),
                async () => await FastTestAsync(counts),
                () => TestTeardown(teardowns));

            Assert.That(output, Is.Not.Empty);
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
        }

        [Test]
        public async Task WritesStressSummaryToConsole()
        {
            var counts = new BlockingCollection<bool>();
            var setups = new BlockingCollection<bool>();
            var teardowns = new BlockingCollection<bool>();

            await stressor.StressAsync(
                () => TestSetup(setups),
                async () => await FastTestAsync(counts),
                () => TestTeardown(teardowns));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(6));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:0"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public async Task WritesFailedStressSummaryToConsole()
        {
            var setups = new BlockingCollection<bool>();
            var teardowns = new BlockingCollection<bool>();

            Assert.That(async () =>
                await stressor.StressAsync(
                    () => setups.Add(true),
                    FailedTestAsync,
                    () => teardowns.Add(true)),
                Throws.InstanceOf<AssertionException>());
            Assert.That(setups, Has.Count.EqualTo(8));
            Assert.That(teardowns, Has.Count.EqualTo(8));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(6));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(output[3], Is.EqualTo($"\tCompleted Iterations: 0 (0%)"));
            Assert.That(output[4], Is.EqualTo($"\tIterations Per Second: 0"));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: FAILED"));
        }

        private async Task FailedTestAsync()
        {
            Assert.Fail("This test should fail");
        }

        [Test]
        public async Task WritesStressSlowSummaryToConsole()
        {
            var counts = new BlockingCollection<bool>();
            var setups = new BlockingCollection<bool>();
            var teardowns = new BlockingCollection<bool>();

            await stressor.StressAsync(
                () => TestSetup(setups),
                async () => await SlowTestAsync(counts),
                () => TestTeardown(teardowns));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(6));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public async Task StressATestWithSetupAndTeardown()
        {
            var counts = new BlockingCollection<bool>();
            var setups = new BlockingCollection<bool>();
            var teardowns = new BlockingCollection<bool>();

            await stressor.StressAsync(
                () => TestSetup(setups),
                async () => await FastTestAsync(counts),
                () => TestTeardown(teardowns));

            Assert.That(counts, Has.Count.AtLeast(10000), "Count");
            Assert.That(setups, Has.Count.AtLeast(10000), "Setup");
            Assert.That(teardowns, Has.Count.AtLeast(10000), "Tear Down");
        }

        private void TestSetup(BlockingCollection<bool> collection)
        {
            collection.Add(true);
        }

        private void TestTeardown(BlockingCollection<bool> collection)
        {
            collection.Add(true);
        }

        [Test]
        public async Task StressAFailedTestWithSetupAndTeardown()
        {
            var counts = new BlockingCollection<bool>();
            var setups = new BlockingCollection<bool>();
            var teardowns = new BlockingCollection<bool>();

            Assert.That(async () =>
                await stressor.StressAsync(
                    () => TestSetup(setups),
                    async () => await FastTestAsync(counts, 9266),
                    () => TestTeardown(teardowns)),
                Throws.InstanceOf<AssertionException>());

            Assert.That(counts, Has.Count.EqualTo(9272), "Count");
            Assert.That(setups, Has.Count.EqualTo(9272), "Setup");
            Assert.That(teardowns, Has.Count.EqualTo(9272), "Tear Down");
        }

        [Test]
        public async Task GenerateWithinStressWithSetupAndTeardownHonorsTimeLimit()
        {
            var counts = new BlockingCollection<bool>();
            var setups = new BlockingCollection<bool>();
            var teardowns = new BlockingCollection<bool>();

            stopwatch.Start();

            await stressor.StressAsync(
                () => TestSetup(setups),
                async () => await TestWithGenerateAsync(counts),
                () => TestTeardown(teardowns));

            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.1).Seconds);
            Assert.That(counts, Has.Count.LessThan(Stressor.ConfidentIterations), "Count");
            Assert.That(setups, Has.Count.LessThan(Stressor.ConfidentIterations), "Setup");
            Assert.That(teardowns, Has.Count.LessThan(Stressor.ConfidentIterations), "Tear Down");
            Assert.That(counts, Has.Count.AtLeast(1000), "Count");
            Assert.That(setups, Has.Count.AtLeast(1000), "Setup");
            Assert.That(teardowns, Has.Count.AtLeast(1000), "Tear Down");
        }

        private async Task TestWithGenerateAsync(BlockingCollection<bool> collection)
        {
            var subcount = 0;

            var result = stressor.Generate(() => subcount++, c => c == 9266);
            Assert.That(result, Is.EqualTo(9266));

            collection.Add(true);
            Assert.That(collection, Has.Count.Positive);
        }

        [TestCase(65)]
        [TestCase(70)]
        public async Task PercentageIsAccurate(int testCount)
        {
            options.IsFullStress = true;
            options.TestCount = testCount;
            options.RunningAssembly = null;

            stressor = new Stressor(options, mockLogger.Object);

            var counts = new BlockingCollection<bool>();
            var setups = new BlockingCollection<bool>();
            var teardowns = new BlockingCollection<bool>();

            await stressor.StressAsync(
                () => TestSetup(setups),
                async () => await SlowTestAsync(counts),
                () => TestTeardown(teardowns));

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
        public async Task PreserveStackTrace()
        {
            var counts = new BlockingCollection<bool>();
            var setups = new BlockingCollection<bool>();
            var teardowns = new BlockingCollection<bool>();

            var exception = Assert.ThrowsAsync<ArgumentException>(async () =>
                await stressor.StressAsync(
                    () => TestSetup(setups),
                    async () => await FailStressAsync(counts),
                    () => TestTeardown(teardowns)));

            Assert.That(counts, Has.Count.EqualTo(16));
            Assert.That(setups, Has.Count.EqualTo(16));
            Assert.That(teardowns, Has.Count.EqualTo(16));
            Assert.That(exception.StackTrace.Trim(), Does.Not.StartsWith("at DnDGen.Stress.Stressor.RunActionAsync"));
        }

        public async Task FailStressAsync(BlockingCollection<bool> collection)
        {
            collection.Add(true);
            if (collection.Count > 10)
                throw new ArgumentException();
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        [TestCase(15)]
        [TestCase(16)]
        public async Task HonorMaxAsyncBatch(int parallel)
        {
            options.IsFullStress = true;
            options.MaxAsyncBatch = parallel;

            stressor = new Stressor(options, mockLogger.Object);

            var counts = new BlockingCollection<bool>();
            var setups = new BlockingCollection<bool>();
            var teardowns = new BlockingCollection<bool>();

            await stressor.StressAsync(
                () => TestSetup(setups),
                async () => await FastTestAsync(counts),
                () => TestTeardown(teardowns));

            var expectedCount = Stressor.ConfidentIterations;
            if (Stressor.ConfidentIterations % parallel != 0)
            {
                expectedCount += parallel - Stressor.ConfidentIterations % parallel;
            }

            Assert.That(counts, Has.Count.EqualTo(expectedCount));
            Assert.That(setups, Has.Count.EqualTo(expectedCount));
            Assert.That(teardowns, Has.Count.EqualTo(expectedCount));
        }
    }
}
