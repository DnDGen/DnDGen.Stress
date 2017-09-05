using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DnDGen.Stress.Tests
{
    [TestFixture]
    public class GenerateTests
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
            Console.WriteLine($"Test {runTestCount} of {runTestTotal} for Generate() method for Stressor completed");
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
