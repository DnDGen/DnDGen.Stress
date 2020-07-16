using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace DnDGen.Stress
{
    [TestFixture]
    public class Stressor
    {
        public readonly bool IsFullStress;
        public readonly int StressTestCount;
        public readonly double TimeLimitPercentage;
        public readonly int MaxAsyncBatch;

        public TimeSpan TimeLimit
        {
            get
            {
                var timeLimitInTicks = timeLimitInSeconds * TimeSpan.TicksPerSecond;
                return new TimeSpan((long)timeLimitInTicks);
            }
        }

        public const int ConfidentIterations = 1000000;
        public const int TravisJobOutputTimeLimit = 10 * 60;
        public const int TravisJobBuildTimeLimit = 50 * 60;

        private readonly double timeLimitInSeconds;
        private readonly Stopwatch stressStopwatch;

        protected int iterations;
        private bool generatedSuccessfully;
        private bool generationFailed;

        protected readonly ILogger logger;

        public Stressor(StressorOptions options)
            : this(options, new Logger())
        {
        }

        public Stressor(StressorOptions options, ILogger logger)
        {
            if (!options.AreValid)
                throw new ArgumentException("Stressor Options are not valid");

            this.logger = logger;

            IsFullStress = options.IsFullStress;
            stressStopwatch = new Stopwatch();

            if (options.RunningAssembly != null)
                StressTestCount = CountStressTestsIn(options.RunningAssembly);
            else if (options.TestCount > 0)
                StressTestCount = options.TestCount;

            TimeLimitPercentage = options.TimeLimitPercentage;
            timeLimitInSeconds = GetTimeLimitInSeconds();

            MaxAsyncBatch = options.MaxAsyncBatch;
        }

        private double GetTimeLimitInSeconds()
        {
            if (StressTestCount == 0)
                throw new ArgumentException("No tests were detected in the running assembly");

            if (!IsFullStress)
            {
                return 1;
            }

            var timeLimitPerTest = TravisJobBuildTimeLimit * TimeLimitPercentage / StressTestCount;
            return Math.Min(timeLimitPerTest, TravisJobOutputTimeLimit * TimeLimitPercentage);
        }

        public static int CountStressTestsIn(Assembly runningAssembly)
        {
            var types = runningAssembly.GetTypes();
            var methods = types.SelectMany(t => t.GetMethods());
            var activeStressTests = methods.Where(m => IsActiveTest(m));
            var stressTestsCount = activeStressTests.Sum(m => m.GetCustomAttributes<TestAttribute>(true).Count());
            var stressTestCasesCount = activeStressTests.Sum(m => m.GetCustomAttributes<TestCaseAttribute>().Count(tc => TestCaseIsActive(tc)));
            var stressTestsTotal = stressTestsCount + stressTestCasesCount;

            return stressTestsTotal;
        }

        private static bool IsActiveTest(MethodInfo method)
        {
            if (method.GetCustomAttributes<IgnoreAttribute>(true).Any())
                return false;

            if (method.GetCustomAttributes<TestAttribute>(true).Any())
                return true;

            return method.GetCustomAttributes<TestCaseAttribute>(true).Any(tc => TestCaseIsActive(tc));
        }

        private static bool TestCaseIsActive(TestCaseAttribute testCase)
        {
            return string.IsNullOrEmpty(testCase.Ignore) && string.IsNullOrEmpty(testCase.IgnoreReason);
        }

        protected virtual void StressSetup()
        {
            iterations = 0;
            generatedSuccessfully = false;
            generationFailed = false;

            logger.Log($"Stress timeout is {TimeLimit}");

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
            var timePercentage = Math.Round(stressStopwatch.Elapsed.TotalSeconds / TimeLimit.TotalSeconds * 100, 2);
            var iterationPercentage = Math.Round((double)iterations / ConfidentIterations * 100, 2);
            var status = IsLikelySuccess(timePercentage, iterationPercentage) ? "PASSED" : "FAILED";

            logger.Log($"Stress test complete");
            logger.Log($"\tTime: {stressStopwatch.Elapsed} ({timePercentage}%)");
            logger.Log($"\tCompleted Iterations: {iterations} ({iterationPercentage}%)");
            logger.Log($"\tIterations Per Second: {iterationsPerSecond}");
            logger.Log($"\tLikely Status: {status}");
        }

        private bool IsLikelySuccess(double timePercentage, double iterationPercentage)
        {
            return (timePercentage >= 100 || iterationPercentage >= 100 || generatedSuccessfully) && !generationFailed;
        }

        public virtual void Stress(Action setup, Action test, Action teardown)
        {
            RunAction(
                StressSetup,
                () => RunInLoop(setup, test, teardown),
                StressTearDown);
        }

        public virtual async Task StressAsync(Action setup, Func<Task> test, Action teardown)
        {
            await RunActionAsync(
                StressSetup,
                async () => await RunInLoopAsync(setup, test, teardown),
                StressTearDown);
        }

        private void RunInLoop(Action setup, Action test, Action teardown)
        {
            do
            {
                RunAction(setup, test, teardown);
            }
            while (TestShouldKeepRunning());
        }

        private async Task RunInLoopAsync(Action setup, Func<Task> test, Action teardown)
        {
            do
            {
                var tasks = new List<Task>(MaxAsyncBatch);

                while (tasks.Count < MaxAsyncBatch)
                {
                    var task = RunActionAsync(setup, test, teardown);
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
            while (TestShouldKeepRunning(MaxAsyncBatch));
        }

        private void RunAction(Action setup, Action action, Action teardown)
        {
            setup();

            try
            {
                action();
            }
            catch (Exception e)
            {
                ExceptionDispatchInfo.Capture(e).Throw();
                return;
            }
            finally
            {
                teardown();
            }
        }

        private async Task RunActionAsync(Action setup, Func<Task> action, Action teardown)
        {
            setup();

            try
            {
                await action();
            }
            catch (Exception e)
            {
                ExceptionDispatchInfo.Capture(e).Throw();
                return;
            }
            finally
            {
                teardown();
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
                ExceptionDispatchInfo.Capture(e).Throw();
                return default(T);
            }
            finally
            {
                StressTearDown();
            }
        }

        private bool TestShouldKeepRunning(int iterationCount = 1)
        {
            iterations += iterationCount;
            return stressStopwatch.Elapsed < TimeLimit && iterations < ConfidentIterations;
        }

        public virtual void Stress(Action test)
        {
            RunAction(StressSetup, () => RunInLoop(test), StressTearDown);
        }

        public virtual async Task StressAsync(Func<Task> test)
        {
            await RunActionAsync(
                StressSetup,
                async () => await RunInLoopAsync(test),
                StressTearDown);
        }

        private void RunInLoop(Action test)
        {
            do
            {
                test();
            }
            while (TestShouldKeepRunning());
        }

        private async Task RunInLoopAsync(Func<Task> test)
        {
            do
            {
                var tasks = new List<Task>(MaxAsyncBatch);

                while (tasks.Count < MaxAsyncBatch)
                {
                    var task = test();
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
            while (TestShouldKeepRunning(MaxAsyncBatch));
        }

        //INFO: We are explicitly not doing the setup or teardown here, as Generate is often used within a Stress or GenerateOrFail call, which will handle the setup
        public virtual T Generate<T>(Func<T> generate, Func<T, bool> isValid)
        {
            T generatedObject;

            do
            {
                generatedObject = generate();
            }
            while (!isValid(generatedObject));

            return generatedObject;
        }

        public virtual T GenerateOrFail<T>(Func<T> generate, Func<T, bool> isValid)
        {
            return RunFunction(() => RunGenerateOrFail(generate, isValid));
        }

        private T RunGenerateOrFail<T>(Func<T> generate, Func<T, bool> isValid)
        {
            T generatedObject;
            var shouldKeepRunning = false;

            do
            {
                generatedObject = generate();
                generatedSuccessfully = isValid(generatedObject);
                shouldKeepRunning = TestShouldKeepRunning();
            }
            while (shouldKeepRunning && !generatedSuccessfully);

            if (!generatedSuccessfully && !shouldKeepRunning)
            {
                generationFailed = true;
                Assert.Fail($"Generation timed out");
            }

            return generatedObject;
        }
    }
}
