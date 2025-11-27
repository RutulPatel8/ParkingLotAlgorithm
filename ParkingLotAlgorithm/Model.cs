namespace ParkingLotAlgorithm
{
    //Input Models
    public record Workload(
        int Id,
        int GroupId,
        int DurationInMinutes,
        int AreaRequired,
        int EffectiveWeight,
        bool IsExclusive,
        DateTime ExpectedStartDate,
        List<int> Dependencies
    );

    public record Resource(
        int Id,
        int MaxArea,
        int MaxLiftWeight,
        int AvailableMinutesPerDay
    );

    public record ResourceAvailability(
        int Id,
        int ResourceId,
        DateTime Date,
        int TotalArea,
        int TotalMinutes
    )
    {
        public int RemainingArea { get; set; } = TotalArea;
        public int RemainingMinutes { get; set; } = TotalMinutes;
        public int Status { get; set; } = 0; // 0 = Available, 1 = Partially Booked, 2 = Fully Booked
    }

    public record SchedulingOptions(
        bool EnableParallelExecution = true,
        bool StrictDependencyMode = true,
        bool PreferLeastLoadedResource = true
    );

    public record BookedAvailability(
        int AvailabilityId,
        DateTime Date,
        int ResourceId,
        int BookedArea,
        int BookedMinutes
    );

    //Output Model
    public record ScheduleResult(
        int WorkloadId,
        DateTime Start,
        DateTime End,
        List<BookedAvailability> Bookings
    );
}
