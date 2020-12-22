using System;
using System.Reflection;

namespace DnDGen.Stress
{
    public class StressorOptions
    {
        public bool IsFullStress { get; set; }
        public int TestCount { get; set; }
        public Assembly RunningAssembly { get; set; }
        public double TimeLimitPercentage { get; set; }
        public int MaxAsyncBatch { get; set; }
        public int OutputTimeLimitInSeconds { get; set; }
        public int BuildTimeLimitInSeconds { get; set; }
        public int ConfidenceIterations { get; set; }

        public const int DefaultOutputTimeLimitInSeconds = 10 * 60;
        public const int DefaultBuildTimeLimitInSeconds = 60 * 60;
        public const int DefaultConfidenceIterations = 1000000;

        public virtual bool AreValid
        {
            get
            {
                return (TestCount > 0 ^ RunningAssembly != null)
                    && TimeLimitPercentage > 0
                    && TimeLimitPercentage <= 1
                    && TestCount >= 0;
            }
        }

        public StressorOptions()
        {
            TimeLimitPercentage = 1;
            MaxAsyncBatch = Environment.ProcessorCount;
            OutputTimeLimitInSeconds = DefaultOutputTimeLimitInSeconds;
            BuildTimeLimitInSeconds = DefaultBuildTimeLimitInSeconds;
            ConfidenceIterations = DefaultConfidenceIterations;
        }
    }
}
