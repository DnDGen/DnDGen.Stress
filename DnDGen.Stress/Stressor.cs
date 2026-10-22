using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DnDGen.Stress
{
    [TestFixture]
    public class Stressor
    {
        public readonly StressorOptions Options;
        public readonly int StressTestCount;

        public TimeSpan TimeLimit
        {
            get
            {
                var timeLimitInSeconds = GetTimeLimitInSeconds();
                return TimeSpan.FromSeconds(timeLimitInSeconds);
            }
        }

        public TimeSpan TestDuration => stressStopwatch.Elapsed;
        public int TestIterations => iterations;

        private readonly Stopwatch stressStopwatch;

        protected int iterations;
        private bool generatedSuccessfully;
        private bool generationFailed;

        protected readonly ILogger logger;

        public Stressor(StressorOptions options)
            : this(options, GetLogger())
        {
        }

        private static ILogger GetLogger()
        {
            using var factory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = factory.CreateLogger<Stressor>();

            return logger;
        }

        public Stressor(StressorOptions options, ILogger logger)
        {
            if (!options.AreValid)
                throw new ArgumentException("Stressor Options are not valid");

            this.logger = logger;
            Options = options;

            stressStopwatch = new Stopwatch();

            if (options.RunningAssembly != null)
                StressTestCount = CountStressTestsIn(options.RunningAssembly);
            else if (options.TestCount > 0)
                StressTestCount = options.TestCount;

            if (StressTestCount == 0)
                throw new ArgumentException("No tests were detected in the running assembly");
        }

        private double GetTimeLimitInSeconds()
        {
            if (!Options.IsFullStress)
            {
                return 1;
            }

            var timeLimitPerTest = Options.BuildTimeLimitInSeconds * Options.TimeLimitPercentage / StressTestCount;
            return Math.Min(timeLimitPerTest, Options.OutputTimeLimitInSeconds * Options.TimeLimitPercentage);
        }

        public static int CountStressTestsIn(Assembly runningAssembly)
        {
            var types = runningAssembly.GetTypes();
            var methods = types.SelectMany(t => t.GetMethods());
            var stressTestsTotal = methods.Sum(CountActiveTestsIn);

            return stressTestsTotal;
        }

        private static int CountActiveTestsIn(MethodInfo method)
        {
            if (method.GetCustomAttributes<IgnoreAttribute>(true).Any())
                return 0;

            var testsCount = method.GetCustomAttributes<TestAttribute>(true).Count();
            var testCasesCount = method.GetCustomAttributes<TestCaseAttribute>(true).Count(TestCaseIsActive);

            return testsCount + testCasesCount;
        }

        private static bool TestCaseIsActive(TestCaseAttribute testCase)
        {
            return string.IsNullOrEmpty(testCase.Ignore) && string.IsNullOrEmpty(testCase.IgnoreReason);
        }

        protected virtual void StressSetup()
        {
            var message = BuildLogMessage(
                $"Beginning stress test '{TestContext.CurrentContext.Test.Name}'",
                $"Stress timeout is {TimeLimit}");
            logger.LogInformation(message);

            iterations = 0;
            generatedSuccessfully = false;
            generationFailed = false;

            stressStopwatch.Restart();
        }

        private static string BuildLogMessage(params string[] lines)
        {
            var builder = new StringBuilder();
            foreach (var line in lines)
                builder.AppendLine(line);

            return builder.ToString();
        }

        protected virtual void StressTearDown()
        {
            stressStopwatch.Stop();

            WriteStressSummary();
        }

        private void WriteStressSummary()
        {
            var iterationsPerSecond = Math.Round(iterations / TestDuration.TotalSeconds, 2);
            var timePercentage = TestDuration.TotalSeconds / TimeLimit.TotalSeconds;
            var iterationPercentage = (double)iterations / Options.ConfidenceIterations;
            var status = IsLikelySuccess(timePercentage, iterationPercentage) ? "PASSED" : "FAILED";

            var message = BuildLogMessage(
                $"Stress test '{TestContext.CurrentContext.Test.Name}' complete",
                $"\tFull Name: {TestContext.CurrentContext.Test.FullName}",
                $"\tTime: {TestDuration} ({timePercentage:P})",
                $"\tCompleted Iterations: {iterations:N0} ({iterationPercentage:P})",
                $"\tIterations Per Second: {iterationsPerSecond:N2}",
                $"\tLikely Status: {status}");
            logger.LogInformation(message);
        }

        private bool IsLikelySuccess(double timePercentage, double iterationPercentage)
        {
            return (timePercentage >= 1 || iterationPercentage >= 1 || generatedSuccessfully) && !generationFailed;
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
                var tasks = new List<Task>(Options.MaxAsyncBatch);

                while (tasks.Count < Options.MaxAsyncBatch)
                {
                    var task = RunActionAsync(setup, test, teardown);
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
            while (TestShouldKeepRunning(Options.MaxAsyncBatch));
        }

        private static void RunAction(Action setup, Action action, Action teardown)
        {
            setup();

            try
            {
                action();
            }
            finally
            {
                teardown();
            }
        }

        private static async Task RunActionAsync(Action setup, Func<Task> action, Action teardown)
        {
            setup();

            try
            {
                await action();
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
            finally
            {
                StressTearDown();
            }
        }

        private bool TestShouldKeepRunning(int iterationCount = 1)
        {
            iterations += iterationCount;
            return TestDuration < TimeLimit && iterations < Options.ConfidenceIterations;
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
                var tasks = new List<Task>(Options.MaxAsyncBatch);

                while (tasks.Count < Options.MaxAsyncBatch)
                {
                    var task = test();
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
            while (TestShouldKeepRunning(Options.MaxAsyncBatch));
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
            bool shouldKeepRunning;
            do
            {
                generatedObject = generate();
                generatedSuccessfully = isValid(generatedObject);
                shouldKeepRunning = TestShouldKeepRunning();
            }
            while (shouldKeepRunning && !generatedSuccessfully);

            //INFO: The loop only exits without a success once it has run out of time or iterations
            generationFailed = !generatedSuccessfully;
            if (generationFailed)
                Assert.Fail("Generation timed out");

            return generatedObject;
        }
    }
}
