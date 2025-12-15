using Google.OrTools.Sat;

// ----------------------
// Workloads
// ----------------------
var workloads = new List<WorkloadTest>
{
    new WorkloadTest(1, 750, 500),
    new WorkloadTest(2, 750, 1000)
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

// amount[j,a] = minutes workload j uses on availability a
IntVar[,] amount = new IntVar[numWorkload, numAvail];

// used[j,a] = 1 if workload j uses availability a
BoolVar[,] used = new BoolVar[numWorkload, numAvail];

// assign[j,r] = workload j assigned to resource r
var resourceIds = availabilities.Select(a => a.ResourceId).Distinct().ToList();
int numResources = resourceIds.Count;
BoolVar[,] assign = new BoolVar[numWorkload, numResources];

List<IntVar> allAssigned = new();

// ----------------------
// Create variables
// ----------------------
for (int j = 0; j < numWorkload; j++)
{
    for (int r = 0; r < numResources; r++)
        assign[j, r] = model.NewBoolVar($"assign_W{j}_R{resourceIds[r]}");

    for (int a = 0; a < numAvail; a++)
    {
        amount[j, a] = model.NewIntVar(
            0,
            availabilities[a].TotalMinutes,
            $"amt_W{j}_A{a}"
        );

        used[j, a] = model.NewBoolVar($"used_W{j}_A{a}");

        allAssigned.Add(amount[j, a]);
    }
}

// ----------------------
// Objective: maximize total assigned minutes
// ----------------------
model.Maximize(LinearExpr.Sum(allAssigned));

// ===================================================
// Constraints
// ===================================================

// -----------------------------
// Each workload selects exactly ONE resource
// -----------------------------
for (int j = 0; j < numWorkload; j++)
{
    model.Add(
        LinearExpr.Sum(
            Enumerable.Range(0, numResources).Select(r => assign[j, r])
        ) == 1
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
// CONTINUITY per resource (no gaps)
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
// Workload duration (partial allowed)
// -----------------------------------------
for (int j = 0; j < numWorkload; j++)
{
    model.Add(
        LinearExpr.Sum(
            Enumerable.Range(0, numAvail).Select(a => amount[j, a])
        ) <= workloads[j].durationInMinutes
    );
}

// -----------------------------------------
// Daily capacity per availability
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

foreach (var workload in workloads)
{
    Console.WriteLine($"Workload-{workload.id} : {workload.durationInMinutes}");
}

if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
{
    // Group availabilities by resource
    var availByResource = availabilities
        .Select((a, idx) => new { Availability = a, Index = idx })
        .GroupBy(x => x.Availability.ResourceId)
        .OrderBy(g => g.Key);

    foreach (var resourceGroup in availByResource)
    {
        int resourceId = resourceGroup.Key;
        Console.WriteLine($"\nResource {resourceId} ({resourceGroup.First().Availability.TotalMinutes} min/day):\n");

        foreach (var workload in workloads.Select((w, j) => new { w, j }))
        {
            Console.WriteLine($"  Workload W{workload.w.id}:");

            int total = 0;

            foreach (var entry in resourceGroup.OrderBy(x => x.Availability.Date))
            {
                int minutes = (int)solver.Value(amount[workload.j, entry.Index]);
                total += minutes;

                if (minutes > 0)
                {
                    Console.WriteLine(
                        $"    Date {entry.Availability.Date}: {minutes} minutes"
                    );
                }
            }

            Console.WriteLine($"    Total on Resource {resourceId}: {total} min\n");
        }

        Console.WriteLine("-----------------------------");
    }
}

