namespace ParkingLotAlgorithm
{
    public class Scheduler
    {
        private readonly SchedulingOptions _options;

        public Scheduler(SchedulingOptions options)
        {
            _options = options;
        }

        public ScheduleResult Run(List<TaskItem> tasks, List<Zone> zones)
        {
            var orderedTasks = OrderTasks(tasks);

            foreach (var task in orderedTasks)
            {
                var candidates = FindFeasibleSlots(task, zones);

                var chosen = ApplyFitPolicy(task, candidates);

                Commit(task, chosen);
            }

            return BuildResult();
        }

        private List<Task> OrderTasks(List<TaskItem> tasks)
        {
            return _options.Mode == SchedulingMode.PortfolioPass
                ? tasks.OrderBy(t => t.Stage).ThenBy(t => t.Pdd).ToList()
                : tasks.OrderBy(t => t.Project.Priority).ThenBy(t => t.Stage).ToList();
        }

        private Slot ApplyFitPolicy(Task task, List<Slot> slots)
        {
            return _options.FitPolicy switch
            {
                FitPolicy.EarliestFit => slots.OrderBy(s => s.Start).FirstOrDefault(),
                FitPolicy.BestFitArea => slots.OrderBy(s => s.RemainingArea).FirstOrDefault(),
                FitPolicy.BestFitLift => slots.OrderBy(s => s.RemainingLift).FirstOrDefault(),
                FitPolicy.MinCrossShop => slots.OrderBy(s => s.CrossShopPenalty).FirstOrDefault(),
                _ => slots.FirstOrDefault()
            };
        }

        public List<Slot> FindFeasibleSlots(TaskItem t, List<Zone> zones)
        {
            var slots = new List<Slot>();

            foreach (var z in zones)
            {
                // Lift & static feasibility
                if (z.EffectiveLiftTonnes < t.WeightToLiftT) continue;
                if (z.ZoneAreaSqM < t.AreaRequired) continue;

                // Respect shop priority if set
                if (Options.ShopPriority?.Any() == true &&
                    !Options.ShopPriority.Contains(z.ShopId)) continue;

                // Explore timeline
                foreach (var start in SchedulingHorizon)
                {
                    var finish = start.AddDays(t.Duration);

                    // CCR constraint
                    if (Options.HonorCCRWindows &&
                        (start < t.MinStart || finish > t.MaxEnd)) continue;

                    // Precedence: FA->Sub->Fab
                    var prev = t.GetPreviousStageTask();
                    if (prev != null && prev.Finish > start) continue;

                    // Zone daily capacity check
                    bool enoughSpace = true;
                    foreach (var day in EachDay(start, finish))
                    {
                        if (z.Calendar.AreaRemaining(day) < t.AreaRequired)
                        {
                            enoughSpace = false;
                            break;
                        }
                    }
                    if (!enoughSpace) continue;

                    // Build candidate slot
                    slots.Add(new Slot
                    {
                        Task = t,
                        Zone = z,
                        Start = start,
                        Finish = finish,
                        RemainingArea = z.Calendar.MinRemainingArea(start, finish),
                        CrossShopPenalty = (t.Project.PrimaryShop != z.ShopId ? 1 : 0)
                    });
                }
            }

            return slots;
        }

    }


}
