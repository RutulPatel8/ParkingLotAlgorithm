// ----------------------
// Workloads
// ----------------------
using Google.OrTools.Sat;

var workloads = new List<WorkloadTest>
        {
            new WorkloadTest(1, 750),
            new WorkloadTest(2, 7500)
        };

// ----------------------
// 2 Resources, each 100 min/day for 10 days
// ----------------------
var resources = new List<ResourceAvailabilityTest>
        {
            new ResourceAvailabilityTest(1, 100),   // R1: 100 min/day
            new ResourceAvailabilityTest(2, 100)    // R2: 100 min/day
        };

int numWorkload = workloads.Count;
int numResources = resources.Count;
int numDays = 10;

// Build model
CpModel model = new CpModel();

// assign[j,r] = workload j is assigned to resource r
BoolVar[,] assign = new BoolVar[numWorkload, numResources];

// amount[j][r][d] = minutes workload j uses on resource r on day d
IntVar[,,] amount = new IntVar[numWorkload, numResources, numDays];

for (int j = 0; j < numWorkload; j++)
{
    for (int r = 0; r < numResources; r++)
    {
        for (int d = 0; d < numDays; d++)
        {
            assign[j, r] = model.NewBoolVar($"assign_W{j}_R{r}");
            amount[j, r, d] = model.NewIntVar(
                0,
                resources[r].TotalMinutes,
                $"amt_W{j}_R{r}_D{d}"
            );
        }
    }
}

//// ----------------------
//// Each job must receive exactly duration minutes total across all days and resources
//// ----------------------
//for (int j = 0; j < numJobs; j++)
//{
//    List<IntVar> parts = new();
//    for (int r = 0; r < numResources; r++)
//        for (int d = 0; d < numDays; d++)
//            parts.Add(amount[j, r, d]);

//    model.Add(LinearExpr.Sum(parts) == workloads[j].durationInMinutes);
//}

// -----------------------------
// Each workload must select EXACTLY ONE resource
// -----------------------------
for (int j = 0; j < numWorkload; j++)
{
    List<BoolVar> choices = new();
    for (int r = 0; r < numResources; r++)
        choices.Add(assign[j, r]);

    model.Add(LinearExpr.Sum(choices) == 1);
}

//// ----------------------
//// Daily capacity per resource cannot exceed 100 minutes
//// ----------------------
//for (int r = 0; r < numResources; r++)
//{
//    for (int d = 0; d < numDays; d++)
//    {
//        List<IntVar> dayLoad = new();
//        for (int j = 0; j < numJobs; j++)
//            dayLoad.Add(amount[j, r, d]);

//        model.Add(LinearExpr.Sum(dayLoad) <= resources[r].TotalMinutes);
//    }
//}


// -----------------------------
// Each workload must select EXACTLY ONE resource
// -----------------------------
//for (int j = 0; j < numWorkload; j++)
//{
//    List<BoolVar> choices = new();
//    for (int r = 0; r < numResources; r++)
//        choices.Add(assign[j, r]);

//    model.Add(LinearExpr.Sum(choices) == 1);
//}

// -----------------------------------------
// If workload j is NOT assigned to resource r
// amount[j,r,d] = 0
// -----------------------------------------
for (int j = 0; j < numWorkload; j++)
{
    for (int r = 0; r < numResources; r++)
    {
        for (int d = 0; d < numDays; d++)
        {
            // amount <= assign * 100
            model.Add(amount[j, r, d] <= resources[r].TotalMinutes * assign[j, r]);
        }
    }
}

// -----------------------------
// Each job must receive exactly its required minutes
// -----------------------------
for (int j = 0; j < numWorkload; j++)
{
    List<IntVar> sumParts = new();

    for (int r = 0; r < numResources; r++)
        for (int d = 0; d < numDays; d++)
            sumParts.Add(amount[j, r, d]);

    model.Add(LinearExpr.Sum(sumParts) == workloads[j].durationInMinutes);
}

// -----------------------------------------
// DAILY CAPACITY: resource r can give only 100 minutes/day
// -----------------------------------------
for (int r = 0; r < numResources; r++)
{
    for (int d = 0; d < numDays; d++)
    {
        List<IntVar> loads = new();

        for (int j = 0; j < numWorkload; j++)
            loads.Add(amount[j, r, d]);

        model.Add(LinearExpr.Sum(loads) <= 100);
    }
}

// ----------------------
// Objective: Just find feasible solution
// ----------------------
//model.Maximize(1);

// Solve
CpSolver solver = new CpSolver();
solver.StringParameters = "max_time_in_seconds:10";

var status = solver.Solve(model);

Console.WriteLine($"Status: {status}\n");

foreach (var workload in workloads)
{
    Console.WriteLine($" Workload-{workload.id} : {workload.durationInMinutes}");
}

if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
{
    for (int r = 0; r < numResources; r++)
    {
        Console.WriteLine($"Resource {resources[r].Id} (100 min/day):\n");

        for (int j = 0; j < numWorkload; j++)
        {
            Console.WriteLine($"  Workload W{workloads[j].id}:");

            int total = 0;
            for (int d = 0; d < numDays; d++)
            {
                try
                {
                    int mins = (int)solver.Value(amount[j, r, d]);
                    total += mins;
                    if (mins > 0)
                        Console.WriteLine($"    Day {d + 1}: {mins} minutes");
                }
                catch (Exception)
                {
                    Console.WriteLine($"    Day {d + 1}: 0 minutes");
                    // ignored
                }

            }
            Console.WriteLine($"    Total on R{resources[r].Id}: {total} min\n");
        }

        Console.WriteLine("-----------------------------");
    }
}

