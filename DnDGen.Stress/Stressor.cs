using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace DnDGen.Stress
{
    [TestFixture]
    public class Stressor
    {
        public readonly bool IsFullStress;
        public readonly int StressTestCount;

        public TimeSpan TimeLimit
        {
            get { return new TimeSpan(0, 0, timeLimitInSeconds); }
        }

        public const int ConfidentIterations = 1000000;
        public const int TravisJobOutputTimeLimit = 10 * 60;
        public const int TravisJobBuildTimeLimit = 50 * 60 - 3 * 60; //INFO: Taking 3 minutes off to account for initial build time before running the stress tests

        private readonly int timeLimitInSeconds;
        private readonly Stopwatch stressStopwatch;

        private int iterations;
        private bool generatedSuccessfully;

        public Stressor(bool isFullStress, Assembly runningAssembly)
        {
            IsFullStress = isFullStress;
            stressStopwatch = new Stopwatch();
            StressTestCount = CountStressTestsIn(runningAssembly);

            if (StressTestCount == 0)
                throw new ArgumentException("No tests were detected in the running assembly");

            if (!IsFullStress)
            {
                timeLimitInSeconds = 1;
            }
            else
            {
                var timeLimitPerTest = TravisJobBuildTimeLimit / StressTestCount;
                timeLimitInSeconds = Math.Min(timeLimitPerTest, TravisJobOutputTimeLimit - 10);
            }
        }

        private int CountStressTestsIn(Assembly runningAssembly)
        {
            var types = runningAssembly.GetTypes();
            var methods = types.SelectMany(t => t.GetMethods());
            var activeStressTests = methods.Where(m => IsActiveTest(m));
            var stressTestsCount = activeStressTests.Sum(m => m.GetCustomAttributes<TestAttribute>(true).Count());
            var stressTestCasesCount = activeStressTests.Sum(m => m.GetCustomAttributes<TestCaseAttribute>().Count(tc => TestCaseIsActive(tc)));
            var stressTestsTotal = stressTestsCount + stressTestCasesCount;

            return stressTestsTotal;
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

        protected virtual void StressSetup()
        {
            iterations = 0;
            generatedSuccessfully = false;

            Console.WriteLine($"Stress timeout is {TimeLimit}");

            stressStopwatch.Start();
        }

        protected virtual void StressTearDown()
        {
            stressStopwatch.Stop();

            WriteStressSummary();

            stressStopwatch.Reset();
        }

        private void WriteStressSummary()
        {
            var iterationsPerSecond = Math.Round(iterations / stressStopwatch.Elapsed.TotalSeconds, 2);
            var timePercentage = Math.Round(stressStopwatch.Elapsed.TotalSeconds / timeLimitInSeconds * 100, 2);
            var iterationPercentage = Math.Round((double)iterations / ConfidentIterations * 100, 2);
            var status = IsLikelySuccess(timePercentage, iterationPercentage) ? "PASSED" : "FAILED";

            Console.WriteLine($"Stress test complete");
            Console.WriteLine($"\tTime: {stressStopwatch.Elapsed} ({timePercentage}%)");
            Console.WriteLine($"\tCompleted Iterations: {iterations} ({iterationPercentage}%)");
            Console.WriteLine($"\tIterations Per Second: {iterationsPerSecond}");
            Console.WriteLine($"\tLikely Status: {status}");
        }

        private bool IsLikelySuccess(double timePercentage, double iterationPercentage)
        {
            return timePercentage >= 100 || iterationPercentage >= 100 || generatedSuccessfully;
        }

        public void Stress(Action setup, Action test, Action teardown)
        {
            RunAction(() => RunStress(setup, test, teardown));
        }

        protected virtual void RunStress(Action setup, Action test, Action teardown)
        {
            do
            {
                setup();
                test();
                teardown();
            }
            while (TestShouldKeepRunning());
        }

        private void RunAction(Action action)
        {
            StressSetup();

            try
            {
                action();
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                StressTearDown();
            }
        }

        private T RunFunction<T>(Func<T> function)
        {
            StressSetup();

            try
            {
                return function();
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                StressTearDown();
            }
        }

        private bool TestShouldKeepRunning()
        {
            iterations++;
            return stressStopwatch.Elapsed.TotalSeconds < timeLimitInSeconds && iterations < ConfidentIterations;
        }

        public void Stress(Action test)
        {
            RunAction(() => RunStress(test));
        }

        protected virtual void RunStress(Action test)
        {
            do
            {
                test();
            }
            while (TestShouldKeepRunning());
        }

        public T Generate<T>(Func<T> generate, Func<T, bool> isValid)
        {
            return RunFunction(() => RunGenerate(generate, isValid));
        }

        protected virtual T RunGenerate<T>(Func<T> generate, Func<T, bool> isValid)
        {
            T generatedObject;

            do
            {
                generatedObject = generate();
            }
            while (!isValid(generatedObject));

            return generatedObject;
        }

        public T GenerateOrFail<T>(Func<T> generate, Func<T, bool> isValid)
        {
            return RunFunction(() => RunGenerateOrFail(generate, isValid));
        }

        protected virtual T RunGenerateOrFail<T>(Func<T> generate, Func<T, bool> isValid)
        {
            T generatedObject;

            do
            {
                generatedObject = generate();
            }
            while (TestShouldKeepRunning() && !isValid(generatedObject));

            if (!TestShouldKeepRunning() && !isValid(generatedObject))
                Assert.Fail($"Generation timed out");

            return generatedObject;
        }
    }
}
