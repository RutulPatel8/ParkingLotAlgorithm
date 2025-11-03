namespace ParkingLotAlgorithm
{
    public enum SchedulingMode { PortfolioPass, ProjectFirst }
    public enum FitPolicy { EarliestFit, BestFitArea, BestFitLift, MinCrossShop }

    public class SchedulingOptions
    {
        public SchedulingMode Mode { get; set; }
        public FitPolicy FitPolicy { get; set; } = FitPolicy.EarliestFit;
        public bool HonorCCRWindows { get; set; } = true;
        public bool LockInProgress { get; set; } = true;
        public bool AllowCrossShop { get; set; } = true;
        public List<string> ShopPriority { get; set; } = new();
    }
}
