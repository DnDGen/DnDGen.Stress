using DnDGen.EventGen;

namespace DnDGen.Stress.Events
{
    public class StressorWithEventsOptions : StressorOptions
    {
        public ClientIDManager ClientIdManager { get; set; }
        public GenEventQueue EventQueue { get; set; }
        public string Source { get; set; }

        public override bool AreValid
        {
            get
            {
                return base.AreValid
                    && ClientIdManager != null
                    && EventQueue != null
                    && !string.IsNullOrWhiteSpace(Source);
            }
        }
    }
}
