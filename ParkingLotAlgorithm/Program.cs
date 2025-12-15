using Google.OrTools.Sat;

// ----------------------
// Workloads
// ----------------------
var workloads = new List<WorkloadTest>
{
    new WorkloadTest(1, 750, 500),
    new WorkloadTest(2, 750, 1000), // impossible workload
    new WorkloadTest(3, 250, 1000) // impossible workload
};

// ----------------------
// Resource availability (resource × date)
// ----------------------
var availabilities = new List<ResourceAvailabilityTest>
{
    new ResourceAvailabilityTest(1, 1, 100, 500),
    new ResourceAvailabilityTest(1, 2, 100, 500),
    new ResourceAvailabilityTest(1, 3, 100, 500),
    new ResourceAvailabilityTest(1, 4, 100, 500),
    new ResourceAvailabilityTest(1, 5, 100, 500),
    new ResourceAvailabilityTest(1, 6, 100, 500),
    new ResourceAvailabilityTest(1, 7, 100, 500),
    new ResourceAvailabilityTest(1, 8, 100, 500),
    new ResourceAvailabilityTest(1, 9, 100, 500),
    new ResourceAvailabilityTest(1,10, 100, 500),

    new ResourceAvailabilityTest(2, 1, 100, 1000),
    new ResourceAvailabilityTest(2, 2, 100, 1000),
    new ResourceAvailabilityTest(2, 3, 100, 1000),
    new ResourceAvailabilityTest(2, 4, 100, 1000),
    new ResourceAvailabilityTest(2, 5, 100, 1000),
    new ResourceAvailabilityTest(2, 6, 100, 1000),
    new ResourceAvailabilityTest(2, 7, 100, 1000),
    new ResourceAvailabilityTest(2, 8, 100, 1000),
    new ResourceAvailabilityTest(2, 9, 100, 1000),
    new ResourceAvailabilityTest(2,10, 100, 1000),
};

int numWorkload = workloads.Count;
int numAvail = availabilities.Count;

// ----------------------
// Build model
// ----------------------
CpModel model = new CpModel();

// ----------------------
// Variables
// ----------------------
IntVar[,] amount = new IntVar[numWorkload, numAvail];
BoolVar[,] used = new BoolVar[numWorkload, numAvail];

var resourceIds = availabilities.Select(a => a.ResourceId).Distinct().ToList();
int numResources = resourceIds.Count;

BoolVar[,] assign = new BoolVar[numWorkload, numResources];
BoolVar[] workloadUsed = new BoolVar[numWorkload];

List<IntVar> allAssigned = new();

// ----------------------
// Create variables
// ----------------------
for (int j = 0; j < numWorkload; j++)
{
    workloadUsed[j] = model.NewBoolVar($"workloadUsed_{j}");

    for (int r = 0; r < numResources; r++)
        assign[j, r] = model.NewBoolVar($"assign_W{j}_R{resourceIds[r]}");

    for (int a = 0; a < numAvail; a++)
    {
        amount[j, a] = model.NewIntVar(0, availabilities[a].TotalMinutes, $"amt_W{j}_A{a}");
        used[j, a] = model.NewBoolVar($"used_W{j}_A{a}");
        allAssigned.Add(amount[j, a]);
    }
}

// ----------------------
// Objective
// ----------------------
model.Maximize(
    LinearExpr.Sum(allAssigned) + 10000 * LinearExpr.Sum(workloadUsed)
);

// ===================================================
// Constraints
// ===================================================

// -----------------------------
// Optional resource assignment
// -----------------------------
for (int j = 0; j < numWorkload; j++)
{
    model.Add(
        LinearExpr.Sum(
            Enumerable.Range(0, numResources).Select(r => assign[j, r])
        ) == workloadUsed[j]
    );
}

// -----------------------------------------
// Link amount ↔ used
// -----------------------------------------
for (int j = 0; j < numWorkload; j++)
{
    for (int a = 0; a < numAvail; a++)
    {
        model.Add(amount[j, a] >= 1).OnlyEnforceIf(used[j, a]);
        model.Add(amount[j, a] == 0).OnlyEnforceIf(used[j, a].Not());
    }
}

// -----------------------------------------
// Enforce resource assignment
// -----------------------------------------
for (int j = 0; j < numWorkload; j++)
{
    for (int a = 0; a < numAvail; a++)
    {
        int rIndex = resourceIds.IndexOf(availabilities[a].ResourceId);
        model.Add(
            amount[j, a] <= availabilities[a].TotalMinutes * assign[j, rIndex]
        );
    }
}

// -----------------------------------------
// LIFT CAPACITY VALIDATION
// -----------------------------------------
var resourceLift = availabilities
    .GroupBy(a => a.ResourceId)
    .ToDictionary(g => g.Key, g => g.First().EffectiveLiftTonnes);

for (int j = 0; j < numWorkload; j++)
{
    for (int r = 0; r < numResources; r++)
    {
        int resourceId = resourceIds[r];
        if (resourceLift[resourceId] < workloads[j].weightToLiftTonnes)
        {
            model.Add(assign[j, r] == 0);
        }
    }
}

// -----------------------------------------
// CONTINUITY (no gaps)
// -----------------------------------------
for (int j = 0; j < numWorkload; j++)
{
    foreach (var rId in resourceIds)
    {
        var seq = availabilities
            .Select((a, idx) => new { a, idx })
            .Where(x => x.a.ResourceId == rId)
            .OrderBy(x => x.a.Date)
            .ToList();

        for (int k = 1; k < seq.Count; k++)
        {
            model.Add(used[j, seq[k].idx] <= used[j, seq[k - 1].idx]);
            model.Add(
                amount[j, seq[k - 1].idx] >=
                seq[k - 1].a.TotalMinutes * used[j, seq[k].idx]
            );
        }
    }
}

// -----------------------------------------
// Workload duration
// -----------------------------------------
for (int j = 0; j < numWorkload; j++)
{
    model.Add(
        LinearExpr.Sum(
            Enumerable.Range(0, numAvail).Select(a => amount[j, a])
        ) <= workloads[j].durationInMinutes * workloadUsed[j]
    );
}

// -----------------------------------------
// Daily capacity
// -----------------------------------------
for (int a = 0; a < numAvail; a++)
{
    model.Add(
        LinearExpr.Sum(
            Enumerable.Range(0, numWorkload).Select(j => amount[j, a])
        ) <= availabilities[a].TotalMinutes
    );
}

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
if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
{
    for (int j = 0; j < numWorkload; j++)
    {
        Console.WriteLine(
            $"Workload {workloads[j].id} " +
            (solver.Value(workloadUsed[j]) == 1 ? "SCHEDULED" : "NOT SCHEDULED")
        );
    }

    var availByResource = availabilities
        .Select((a, idx) => new { Availability = a, Index = idx })
        .GroupBy(x => x.Availability.ResourceId);

    foreach (var resourceGroup in availByResource)
    {
        Console.WriteLine($"\nResource {resourceGroup.Key}");

        foreach (var workload in workloads.Select((w, j) => new { w, j }))
        {
            int total = 0;

            foreach (var entry in resourceGroup.OrderBy(x => x.Availability.Date))
            {
                int minutes = (int)solver.Value(amount[workload.j, entry.Index]);
                total += minutes;

                if (minutes > 0)
                    Console.WriteLine($"  W{workload.w.id} - Day {entry.Availability.Date}: {minutes} min");
            }

            if (total > 0)
                Console.WriteLine($"  TOTAL W{workload.w.id}: {total} min");
        }
    }
}