namespace InvoiceGeneratorFromWZ.Contracts.Settings
{
    public class WZGenerationTimes
    {
        public string Courier { get; set; }
        public int StartHour { get; set; }
        public bool IsFallback { get; set; }
    }
}
