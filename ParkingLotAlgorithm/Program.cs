// ----------------------
// Workloads
// ----------------------
using Google.OrTools.Sat;

var workloads = new List<WorkloadTest>
        {
            new WorkloadTest(1, 200, 500),
            new WorkloadTest(2, 300, 1000),
            new WorkloadTest(3, 200, 1000),
            new WorkloadTest(4, 200, 1000),
        };

// ----------------------
// Resource availability (GLOBAL DAYS)
// Resource 2 missing Day 9 & 10
// ----------------------
var availabilities = new List<ResourceAvailabilityTest>();

for (int d = 1; d <= 13; d++)
    availabilities.Add(new ResourceAvailabilityTest(1, d, 100, 500));

for (int d = 1; d <= 8; d++)
    availabilities.Add(new ResourceAvailabilityTest(2, d, 100, 1000));

for (int d = 11; d <= 13; d++)
    availabilities.Add(new ResourceAvailabilityTest(2, d, 100, 1000));

// ----------------------
// Dependencies (index-based)
// Workload 1 must finish before Workload 2
// ----------------------
var dependencies = new List<(int before, int after)>
        {
            (0, 1),
            (2, 3)
        };

// ----------------------
// Build model
// ----------------------
CpModel model = new CpModel();

int horizon = availabilities.Max(a => a.Date) + 10;

var resources = availabilities
    .GroupBy(a => a.ResourceId)
    .ToDictionary(
        g => g.Key,
        g => g.OrderBy(x => x.Date).ToList()
    );

var intervals = new Dictionary<(int w, int r), IntervalVar>();
var presence = new Dictionary<(int w, int r), BoolVar>();

// ----------------------
// Create workload intervals (GLOBAL TIME)
// ----------------------
for (int w = 0; w < workloads.Count; w++)
{
    var workload = workloads[w];

    foreach (var r in resources)
    {
        int resourceId = r.Key;
        var days = r.Value;

        if (days.All(d => d.EffectiveLiftTonnes < workload.weightToLiftTonnes))
            continue;

        int minutesPerDay = days.First().TotalMinutes;
        int requiredDays = (int)Math.Ceiling(
            workload.durationInMinutes / (double)minutesPerDay
        );

        var start = model.NewIntVar(0, horizon, $"start_W{w}_R{resourceId}");
        var end = model.NewIntVar(0, horizon + requiredDays, $"end_W{w}_R{resourceId}");
        var pres = model.NewBoolVar($"pres_W{w}_R{resourceId}");

        var interval = model.NewOptionalIntervalVar(
            start,
            requiredDays,
            end,
            pres,
            $"interval_W{w}_R{resourceId}"
        );

        intervals[(w, resourceId)] = interval;
        presence[(w, resourceId)] = pres;
    }

    // At most one resource per workload
    model.Add(
        LinearExpr.Sum(
            presence.Where(p => p.Key.w == w).Select(p => p.Value)
        ) <= 1
    );
}

// ----------------------
// Block unavailable days per resource
// ----------------------
var blockedIntervals = new Dictionary<int, List<IntervalVar>>();

foreach (var r in resources)
{
    int resourceId = r.Key;
    var availableDays = r.Value.Select(d => d.Date).ToHashSet();

    blockedIntervals[resourceId] = new List<IntervalVar>();

    for (int d = 1; d <= horizon; d++)
    {
        if (!availableDays.Contains(d))
        {
            var blocked = model.NewIntervalVar(
                d,
                1,
                d + 1,
                $"blocked_R{resourceId}_D{d}"
            );
            blockedIntervals[resourceId].Add(blocked);
        }
    }
}

// ----------------------
// No overlap per resource
// ----------------------
foreach (var r in resources)
{
    int resourceId = r.Key;

    var allIntervals = intervals
        .Where(x => x.Key.r == resourceId)
        .Select(x => x.Value)
        .ToList();

    allIntervals.AddRange(blockedIntervals[resourceId]);

    model.AddNoOverlap(allIntervals);
}

// ----------------------
// Dependencies
// ----------------------
foreach (var dep in dependencies)
{
    int wBefore = dep.before;
    int wAfter = dep.after;

    foreach (var rBefore in resources.Keys)
    {
        foreach (var rAfter in resources.Keys)
        {
            if (!intervals.ContainsKey((wBefore, rBefore)))
                continue;

            if (!intervals.ContainsKey((wAfter, rAfter)))
                continue;

            var intervalBefore = intervals[(wBefore, rBefore)];
            var intervalAfter = intervals[(wAfter, rAfter)];

            var presBefore = presence[(wBefore, rBefore)];
            var presAfter = presence[(wAfter, rAfter)];

            // end_before <= start_after
            model.Add(
                intervalBefore.EndExpr() <= intervalAfter.StartExpr()
            )
            .OnlyEnforceIf(new[] { presBefore, presAfter });
        }
    }
}


// ----------------------
// Objective: maximize scheduled workloads
// ----------------------
model.Maximize(LinearExpr.Sum(presence.Values));

// ----------------------
// Solve
// ----------------------
CpSolver solver = new CpSolver();
solver.StringParameters = "max_time_in_seconds:10";
var status = solver.Solve(model);

Console.WriteLine($"Status: {status}\n");

// ----------------------
// Output
// ----------------------
foreach (var w in workloads.Select((w, i) => new { w, i }))
{
    bool scheduled = presence
        .Where(p => p.Key.w == w.i)
        .Any(p => solver.Value(p.Value) == 1);

    Console.WriteLine(
        $"Workload {w.w.id} {(scheduled ? "SCHEDULED" : "NOT SCHEDULED")}"
    );
}

Console.WriteLine("\n==============================");
Console.WriteLine("SCHEDULE PER RESOURCE (GLOBAL DAYS)");
Console.WriteLine("==============================\n");

foreach (var r in resources.Keys)
{
    Console.WriteLine($"Resource {r}");

    foreach (var kv in intervals.Where(x => x.Key.r == r))
    {
        if (solver.Value(presence[kv.Key]) == 0)
            continue;

        int start = (int)solver.Value(kv.Value.StartExpr());
        int end = (int)solver.Value(kv.Value.EndExpr());

        Console.WriteLine(
            $"  Workload {workloads[kv.Key.w].id}: Day {start} → Day {end - 1}"
        );
    }

    Console.WriteLine();
}