using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace DnDGen.Stress.Tests
{
    [TestFixture]
    public class StressTests
    {
        private Stressor stressor;
        private StressorOptions options;
        private Stopwatch stopwatch;
        private StringBuilder console;
        private int runTestCount;
        private int runTestTotal;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            runTestCount = 0;
            runTestTotal = CountTotalTests();
        }

        private int CountTotalTests()
        {
            var type = GetType();
            var methods = type.GetMethods();
            var activeStressTests = methods.Where(m => IsActiveTest(m));
            var testsCount = activeStressTests.Sum(m => m.GetCustomAttributes<TestAttribute>(true).Count());
            var testCasesCount = activeStressTests.Sum(m => m.GetCustomAttributes<TestCaseAttribute>().Count(tc => TestCaseIsActive(tc)));
            var testsTotal = testsCount + testCasesCount;

            return testsTotal;
        }

        private bool IsActiveTest(MethodInfo method)
        {
            if (method.GetCustomAttributes<IgnoreAttribute>(true).Any())
                return false;

            if (method.GetCustomAttributes<TestAttribute>(true).Any())
                return true;

            return method.GetCustomAttributes<TestCaseAttribute>(true).Any(tc => TestCaseIsActive(tc));
        }

        private bool TestCaseIsActive(TestCaseAttribute testCase)
        {
            return string.IsNullOrEmpty(testCase.Ignore) && string.IsNullOrEmpty(testCase.IgnoreReason);
        }

        [SetUp]
        public void Setup()
        {
            options = new StressorOptions();
            options.RunningAssembly = Assembly.GetExecutingAssembly();

            stressor = new Stressor(options);
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

            runTestCount++;
            Console.WriteLine($"Test {runTestCount} of {runTestTotal} for Stress() method for Stressor completed");
        }

        [Test]
        public void StopsWhenTimeLimitHit()
        {
            var count = 0;

            stopwatch.Start();
            stressor.Stress(() => SlowTest(ref count));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.01).Seconds);
            Assert.That(count, Is.LessThan(Stressor.ConfidentIterations));
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

            stressor = new Stressor(options);

            var count = 0;

            stopwatch.Start();
            stressor.Stress(() => FastTest(ref count));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit));
            Assert.That(count, Is.EqualTo(Stressor.ConfidentIterations));
        }

        [Test]
        public void WritesStressDurationToConsole()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
        }

        [Test]
        public void WritesStressSummaryToConsole()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(6));
            Assert.That(lines[1], Is.EqualTo($"Stress test complete"));
            Assert.That(lines[2], Does.StartWith($"\tTime: 00:00:0"));
            Assert.That(lines[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(lines[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(lines[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public void WritesFailedStressSummaryToConsole()
        {
            Assert.That(() => stressor.Stress(FailedTest), Throws.InstanceOf<AssertionException>());

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(6));
            Assert.That(lines[1], Is.EqualTo($"Stress test complete"));
            Assert.That(lines[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(lines[3], Is.EqualTo($"\tCompleted Iterations: 0 (0%)"));
            Assert.That(lines[4], Is.EqualTo($"\tIterations Per Second: 0"));
            Assert.That(lines[5], Is.EqualTo($"\tLikely Status: FAILED"));
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

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(6));
            Assert.That(lines[1], Is.EqualTo($"Stress test complete"));
            Assert.That(lines[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(lines[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(lines[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(lines[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public void StressATest()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));
            Assert.That(count, Is.AtLeast(100000));
        }

        [Test]
        public void GenerateWithinStressHonorsTimeLimit()
        {
            var count = 0;

            stopwatch.Start();
            stressor.Stress(() => TestWithGenerate(ref count));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit).Or.EqualTo(stressor.TimeLimit).Within(.1).Seconds);
            Assert.That(count, Is.LessThan(Stressor.ConfidentIterations));
        }

        private void TestWithGenerate(ref int count)
        {
            var innerCount = count;
            count = stressor.Generate(() => innerCount++, c => c > 1);
            Assert.That(count, Is.AtLeast(2));
        }

        [TestCase(65)]
        [TestCase(70)]
        public void PercentageIsAccurate(int testCount)
        {
            options.IsFullStress = true;
            options.TestCount = testCount;
            options.RunningAssembly = null;

            stressor = new Stressor(options);

            var count = 0;
            stressor.Stress(() => SlowTest(ref count));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var time = stressor.TimeLimit.ToString().Substring(0, 10);

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(6));
            Assert.That(lines[1], Is.EqualTo($"Stress test complete"));
            Assert.That(lines[2], Does.StartWith($"\tTime: {time}"));
            Assert.That(lines[2], Does.Contain($"(100"));
            Assert.That(lines[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(lines[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(lines[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public void PreserveStackTrace()
        {
            var count = 0;

            var exception = Assert.Throws<ArgumentException>(() => stressor.Stress(() => FailStress(count++)));
            Assert.That(count, Is.EqualTo(12));
            Assert.That(exception.StackTrace.Trim(), Does.Not.StartsWith("at DnDGen.Stress.Stressor.RunAction(Action setup, Action action, Action teardown)"));
        }

        public void FailStress(int count)
        {
            if (count > 10)
                throw new ArgumentException();
        }
    }
}
