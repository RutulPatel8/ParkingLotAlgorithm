namespace ParkingLotAlgorithm
{
    public static class TestCases
    {
        /// <summary> Complex test case with multiple workloads, dependencies, and resources </summary>
        public static void ExecuteTestCase1()
        {
            var workloads = new List<Workload>
            {
                // Group 1
                new Workload(1, GroupId: 1, DurationInMinutes: 600, AreaRequired: 650, EffectiveWeight: 10, IsExclusive: false, ExpectedStartDate: DateTime.Today, Dependencies: new List<int>()),
                new Workload(2, GroupId: 1, DurationInMinutes: 480, AreaRequired: 650, EffectiveWeight: 50, IsExclusive: false, ExpectedStartDate: DateTime.Today, Dependencies: new List<int>{1}),
                new Workload(3, GroupId: 1, DurationInMinutes: 720, AreaRequired: 200, EffectiveWeight: 20, IsExclusive: false, ExpectedStartDate: DateTime.Today, Dependencies: new List<int>{2}),
            
                // Group 2
                new Workload(10, GroupId: 2, DurationInMinutes: 420, AreaRequired: 200, EffectiveWeight: 10, IsExclusive: false, ExpectedStartDate: DateTime.Today, Dependencies: new List<int>()),
                new Workload(11, GroupId: 2, DurationInMinutes: 480, AreaRequired: 650, EffectiveWeight: 15, IsExclusive: false, ExpectedStartDate: DateTime.Today, Dependencies: new List<int>{10}),
                new Workload(12, GroupId: 2, DurationInMinutes: 780, AreaRequired: 200, EffectiveWeight: 5, IsExclusive: false, ExpectedStartDate: DateTime.Today, Dependencies: new List<int>())
            };


            var resources = new List<Resource>
            {
                new Resource(Id: 1, MaxArea: 650, MaxLiftWeight: 50, AvailableMinutesPerDay: 720),
                new Resource(Id: 2, MaxArea: 200, MaxLiftWeight: 20, AvailableMinutesPerDay: 720)
            };


            var resourceAvailabilities = new List<ResourceAvailability>
            {
                // Resource 1 - 5 days starting today
                new ResourceAvailability(Id: 1,  ResourceId: 1, Date: DateTime.Today.AddDays(0), TotalArea: 650, TotalMinutes: 720),
                new ResourceAvailability(Id: 2,  ResourceId: 1, Date: DateTime.Today.AddDays(1), TotalArea: 650, TotalMinutes: 720),
                new ResourceAvailability(Id: 3,  ResourceId: 1, Date: DateTime.Today.AddDays(2), TotalArea: 650, TotalMinutes: 720),
                new ResourceAvailability(Id: 4,  ResourceId: 1, Date: DateTime.Today.AddDays(3), TotalArea: 650, TotalMinutes: 720),
                new ResourceAvailability(Id: 5,  ResourceId: 1, Date: DateTime.Today.AddDays(4), TotalArea: 650, TotalMinutes: 720),
            
                // Resource 2 - 5 days starting today
                new ResourceAvailability(Id: 6,  ResourceId: 2, Date: DateTime.Today.AddDays(0), TotalArea: 700, TotalMinutes: 720),
                new ResourceAvailability(Id: 7,  ResourceId: 2, Date: DateTime.Today.AddDays(1), TotalArea: 700, TotalMinutes: 720),
                new ResourceAvailability(Id: 8,  ResourceId: 2, Date: DateTime.Today.AddDays(2), TotalArea: 700, TotalMinutes: 720),
                new ResourceAvailability(Id: 9,  ResourceId: 2, Date: DateTime.Today.AddDays(3), TotalArea: 700, TotalMinutes: 720),
                new ResourceAvailability(Id: 10, ResourceId: 2, Date: DateTime.Today.AddDays(4), TotalArea: 700, TotalMinutes: 720)
            };

            //var sorted = Scheduling.ArrangeWorkload(workloads);

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

            ShowResult(workloads, scheduled);
        }

        /// <summary> Multiple workloads that can be scheduled in parallel on a single resource </summary>
        public static void ExecuteTestCase2()
        {
            // Implement another test case as needed
            var workloads = new List<Workload>
            {   
                new Workload(1, GroupId: 1, DurationInMinutes: 480, AreaRequired: 300, EffectiveWeight: 10, IsExclusive: false, ExpectedStartDate: DateTime.Today, Dependencies: new List<int>()),
                new Workload(2, GroupId: 1, DurationInMinutes: 480, AreaRequired: 300, EffectiveWeight: 10, IsExclusive: false, ExpectedStartDate: DateTime.Today, Dependencies: new List<int>())
            };


            var resources = new List<Resource>
            {
                new Resource(Id: 1, MaxArea: 650, MaxLiftWeight: 50, AvailableMinutesPerDay: 720)/*,*/
            };

            var resourceAvailabilities = new List<ResourceAvailability>
            {
                new ResourceAvailability(Id: 1,  ResourceId: 1, Date: DateTime.Today.AddDays(0), TotalArea: 650, TotalMinutes: 480)
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

            ShowResult(workloads, scheduled);
        }

        /// <summary> Workloads with dependencies that must be respected (1 Day - 2 workload booking process) </summary>
        public static void ExecuteTestCase3()
        {
            // Implement another test case as needed
            var workloads = new List<Workload>
            {
                new Workload(1, GroupId: 1, DurationInMinutes: 480, AreaRequired: 300, EffectiveWeight: 10, IsExclusive: false, ExpectedStartDate: DateTime.Today, Dependencies: new List<int>()),
                new Workload(2, GroupId: 1, DurationInMinutes: 480, AreaRequired: 300, EffectiveWeight: 50, IsExclusive: false, ExpectedStartDate: DateTime.Today, Dependencies: new List<int>{1})
            };
            
            
            var resources = new List<Resource>
            {
                new Resource(Id: 1, MaxArea: 650, MaxLiftWeight: 50, AvailableMinutesPerDay: 720)
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

            ShowResult(workloads, scheduled);
        }

        public static void ShowResult(List<Workload> workloads, List<ScheduleResult> scheduled)
        {
            foreach (var result in scheduled)
            {
                Workload wl = workloads.First(w => w.Id == result.WorkloadId);
                Console.WriteLine($"WorkloadId: {result.WorkloadId}");
                Console.WriteLine($"CCRStart: {result.Start}");
                Console.WriteLine($"CCREnd: {result.End}");
                Console.WriteLine($"AreaRequired: {wl.AreaRequired}");
                Console.WriteLine($"EffectiveWeight: {wl.EffectiveWeight}");
                Console.WriteLine($"DurationInMinutes: {wl.DurationInMinutes}");
                Console.WriteLine("Bookings:");

                foreach (var booking in result.Bookings)
                {
                    Console.WriteLine($"  - {booking}"); // Adjust based on BookedAvailability structure
                }
                Console.WriteLine(); // Empty line between results
            }
        }
    }
}
