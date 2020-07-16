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
            MaxAsyncBatch = 8;
        }
    }
}
