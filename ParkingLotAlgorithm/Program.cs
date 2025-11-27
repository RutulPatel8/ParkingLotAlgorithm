using ParkingLotAlgorithm;

TestCases.ExecuteTestCase3();

var workloads = new List<Workload>
{
    new Workload(1, GroupId: 1, DurationInMinutes: 480, AreaRequired: 650, EffectiveWeight: 10, IsExclusive: false, ExpectedStartDate: DateTime.Today, Dependencies: new List<int>()),
    new Workload(2, GroupId: 1, DurationInMinutes: 480, AreaRequired: 650, EffectiveWeight: 50, IsExclusive: false, ExpectedStartDate: DateTime.Today, Dependencies: new List<int>{1})
};


var resources = new List<Resource>
{
    new Resource(Id: 1, MaxArea: 650, MaxLiftWeight: 50, AvailableMinutesPerDay: 960)/*,*/
    //new Resource(Id: 2, MaxArea: 200, MaxLiftWeight: 20, AvailableMinutesPerDay: 720)
};

var resourceAvailabilities = new List<ResourceAvailability>
{
    new ResourceAvailability(Id: 1,  ResourceId: 1, Date: DateTime.Today.AddDays(0), TotalArea: 650, TotalMinutes: 960)
};

var scheduled = Scheduling.ScheduleAll(
    workloads,
    resources,
    resourceAvailabilities,
    new SchedulingOptions(
        EnableParallelExecution: true,
        StrictDependencyMode: true,
        PreferLeastLoadedResource: true
    )
);

ShowResult(scheduled);

static void ShowResult(List<ScheduleResult> scheduled)
{
    foreach (var result in scheduled)
    {
        Console.WriteLine($"WorkloadId: {result.WorkloadId}");
        Console.WriteLine($"Start: {result.Start}");
        Console.WriteLine($"End: {result.End}");
        Console.WriteLine("Bookings:");

        foreach (var booking in result.Bookings)
        {
            Console.WriteLine($"  - {booking}"); // Adjust based on BookedAvailability structure
        }
        Console.WriteLine(); // Empty line between results
    }
}

