using Google.OrTools.Sat;


// ----------------------
// Workloads
// ----------------------
var workloads = new List<WorkloadTest>
        {
            new WorkloadTest(1, 750, 5000),
            new WorkloadTest(2, 750, 5000),
            new WorkloadTest(3, 250, 5000)
        };

// ----------------------
// Resource availability
// ----------------------
var availabilities = new List<ResourceAvailabilityTest>();

for (int d = 1; d <= 10; d++)
    availabilities.Add(new ResourceAvailabilityTest(1, d, 100, 500));

for (int d = 1; d <= 13; d++)
    availabilities.Add(new ResourceAvailabilityTest(2, d, 100, 5000));

// ----------------------
// Build model
// ----------------------
CpModel model = new CpModel();

var resources = availabilities
    .GroupBy(a => a.ResourceId)
    .ToDictionary(
        g => g.Key,
        g => g.OrderBy(x => x.Date).ToList()
    );

var intervals = new Dictionary<(int w, int r), IntervalVar>();
var presence = new Dictionary<(int w, int r), BoolVar>();

// ----------------------
// Create intervals
// ----------------------
for (int w = 0; w < workloads.Count; w++)
{
    var workload = workloads[w];

    foreach (var r in resources)
    {
        int resourceId = r.Key;
        var days = r.Value;

        // Lift validation
        if (days[0].EffectiveLiftTonnes < workload.weightToLiftTonnes)
            continue;

        int dayMinutes = days[0].TotalMinutes;
        int maxSlots = days.Count;
        int requiredSlots = (int)Math.Ceiling(
            workload.durationInMinutes / (double)dayMinutes
        );

        if (requiredSlots > maxSlots)
            continue;

        var start = model.NewIntVar(0, maxSlots - 1, $"start_W{w}_R{resourceId}");
        var length = model.NewIntVar(requiredSlots, requiredSlots, $"len_W{w}_R{resourceId}");
        var end = model.NewIntVar(0, maxSlots, $"end_W{w}_R{resourceId}");
        var pres = model.NewBoolVar($"pres_W{w}_R{resourceId}");

        var interval = model.NewOptionalIntervalVar(
            start, length, end, pres,
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
// Resource capacity (No overlap)
// ----------------------
foreach (var r in resources)
{
    int resourceId = r.Key;

    var resourceIntervals = intervals
        .Where(x => x.Key.r == resourceId)
        .Select(x => x.Value)
        .ToList();

    model.AddNoOverlap(resourceIntervals);
}

// ----------------------
// Objective: maximize scheduled workloads
// ----------------------
model.Maximize(
    LinearExpr.Sum(presence.Values) * 1000
);

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

Console.WriteLine();

foreach (var kv in intervals)
{
    if (solver.Value(presence[kv.Key]) == 0)
        continue;

    int w = kv.Key.w;
    int r = kv.Key.r;

    int start = (int)solver.Value(kv.Value.StartExpr());
    int len = (int)solver.Value(kv.Value.SizeExpr());

    Console.WriteLine(
        $"Workload {workloads[w].id} → Resource {r} " +
        $"from Day {resources[r][start].Date} " +
        $"to Day {resources[r][start + len - 1].Date}"
    );
}

