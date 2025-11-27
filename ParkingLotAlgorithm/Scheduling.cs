using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingLotAlgorithm
{
    // --- Scheduling class with Step 1 implemented ---
    public static class Scheduling
    {
        /// <summary>
        /// Groups workloads by GroupId (ascending by GroupId) and inside each group
        /// returns workloads in an order that respects dependencies (topological order).
        /// Dependencies that point to workloads outside the same group are ignored for ordering
        /// but validated (missing ids cause exception).
        /// </summary>
        /// <param name="workloads">All workloads to group & sort</param>
        /// <returns>Flattened list: groups sorted by GroupId, each group topologically ordered</returns>
        public static List<(int GroupId, List<Workload> Workloads)> ArrangeWorkload(IEnumerable<Workload> workloads)
        {
            if (workloads == null)
                throw new ArgumentNullException(nameof(workloads));

            var allWorkloads = workloads.ToList();
            var byId = allWorkloads.ToDictionary(w => w.Id);

            return allWorkloads
                .GroupBy(w => w.GroupId)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var ordered = TopologicalSortGroup(g.ToList(), byId);
                    return (g.Key, ordered);
                })
                .ToList();
        }


        /// <summary>
        /// Performs a topological sort for workloads belonging to the same group.
        /// Only dependencies that reference workloads within this group are used for ordering.
        /// If a dependency id is referenced but not found in the global workload set, throws.
        /// If a cycle is detected within the group, throws.
        /// </summary>
        private static List<Workload> TopologicalSortGroup(List<Workload> groupWorkloads, Dictionary<int, Workload> globalById)
        {
            var nodeById = groupWorkloads.ToDictionary(w => w.Id);
            var inDegree = new Dictionary<int, int>();
            var adjacency = new Dictionary<int, List<int>>();

            // initialize
            foreach (var w in groupWorkloads)
            {
                inDegree[w.Id] = 0;
                adjacency[w.Id] = new List<int>();
            }

            // build edges: for each dependency D in workload W, create edge D -> W
            foreach (var w in groupWorkloads)
            {
                if (w.Dependencies == null) continue;

                foreach (var depId in w.Dependencies)
                {
                    // validate dependency refers to an existing workload in the global set
                    if (!globalById.ContainsKey(depId))
                    {
                        throw new InvalidOperationException($"Workload {w.Id} depends on missing workload id {depId}.");
                    }

                    // Only consider dependencies inside the same group for ordering
                    if (!nodeById.ContainsKey(depId))
                    {
                        // dependency is outside the group: ignore for strict ordering inside this group
                        continue;
                    }

                    adjacency[depId].Add(w.Id);
                    inDegree[w.Id] = inDegree[w.Id] + 1;
                }
            }

            // Kahn's algorithm
            var queue = new Queue<int>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            var sortedIds = new List<int>();

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                sortedIds.Add(id);

                foreach (var neighbor in adjacency[id])
                {
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0)
                        queue.Enqueue(neighbor);
                }
            }

            // If not all nodes processed -> cycle inside this group
            if (sortedIds.Count != groupWorkloads.Count)
            {
                // find nodes in cycle (those with inDegree > 0)
                var nodesInCycle = inDegree.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
                var cycleDesc = string.Join(", ", nodesInCycle);
                throw new InvalidOperationException($"Cycle detected in group {groupWorkloads.First().GroupId}. Involved workload ids: {cycleDesc}");
            }

            // Return workloads in sorted order. For deterministic tie-breaking (same in-degree order),
            // we used Queue seeded from inDegree==0 in insertion order; if you'd rather stable-sort,
            // we can seed queue using ascending id order or ExpectedStartDate, etc.
            return sortedIds.Select(id => nodeById[id]).ToList();
        }

        /// <summary>
        /// Top-level scheduler: attempts to schedule every workload (grouped & ordered).
        /// This mutates the resourceAvailabilities (RemainingMinutes/RemainingArea).
        /// Returns the list of ScheduleResult for successfully scheduled workloads.
        /// Throws if a workload cannot be scheduled (you can change to return partial results if preferred).
        /// </summary>
        public static List<ScheduleResult> ScheduleAll(
            IEnumerable<Workload> workloads,
            List<Resource> resources,
            List<ResourceAvailability> resourceAvailabilities,
            SchedulingOptions options)
        {
            if (workloads == null) throw new ArgumentNullException(nameof(workloads));
            if (resources == null) throw new ArgumentNullException(nameof(resources));
            if (resourceAvailabilities == null) throw new ArgumentNullException(nameof(resourceAvailabilities));
            options ??= new SchedulingOptions();

            // Scheduled results accumulator
            var scheduleResults = new List<ScheduleResult>();

            var orderedWorkloads = ArrangeWorkload(workloads);

            // Helper: for resource load preference, compute remaining minutes across availabilities
            Func<Resource, int> resourceLoadKey = r =>
            {
                var totalRemaining = resourceAvailabilities
                    .Where(a => a.ResourceId == r.Id)
                    .Sum(a => a.RemainingMinutes);
                return totalRemaining;
            };

            foreach (var group in orderedWorkloads)
            {
                foreach (var workload in group.Workloads)
                {
                    // earliest start based on expected date and dependency completions
                    var earliest = GetEarliestStartConsideringDependencies(workload, scheduleResults);

                    // candidate resources ordering
                    var candidateResources = options.PreferLeastLoadedResource
                        ? resources.OrderByDescending(resourceLoadKey).ToList()
                        : resources.OrderBy(r => r.Id).ToList();


                    ScheduleResult scheduled = null;
                    Exception lastEx = null;

                    foreach (var res in candidateResources)
                    {
                        try
                        {
                            // resource must satisfy lift weight
                            if (res.MaxLiftWeight < workload.EffectiveWeight) continue;

                            // attempt to find consecutive availability block on this resource
                            //var block = FindConsecutiveAvailabilityBlockForWorkload(workload, res, resourceAvailabilities, earliest, options);
                            var block = FindContinuousSlot(workload, res, resourceAvailabilities, earliest, options);

                            if (block != null && block.Any())
                            {
                                // allocate (mutates resourceAvailabilities)
                                scheduled = AllocateWorkloadOnBlock(workload, res, block);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            lastEx = ex;
                        }
                    }

                    if (scheduled == null)
                    {
                        // cannot schedule this workload with current availabilities
                        var msg = $"Unable to schedule workload {workload.Id} (Group {workload.GroupId}) starting on/after {earliest:d}.";
                        if (lastEx != null) msg += $" Last error: {lastEx.Message}";
                        throw new InvalidOperationException(msg);
                    }

                    scheduleResults.Add(scheduled);
                }
            }

            return scheduleResults;
        }

        /// <summary>
        /// For a workload, returns the earliest DateTime it may start based on ExpectedStartDate and dependency completion.
        /// If dependencies are not scheduled yet, they must be present in scheduleResults (caller should schedule in topological order).
        /// </summary>
        public static DateTime GetEarliestStartConsideringDependencies(Workload workload, List<ScheduleResult> scheduleResults)
        {
            if (workload == null) throw new ArgumentNullException(nameof(workload));
            if (scheduleResults == null) throw new ArgumentNullException(nameof(scheduleResults));

            var earliest = workload.ExpectedStartDate.Date;

            if (workload.Dependencies == null || workload.Dependencies.Count == 0)
                return earliest;

            // find end times for dependencies
            var depEnds = new List<DateTime>();
            foreach (var depId in workload.Dependencies)
            {
                var depSchedule = scheduleResults.FirstOrDefault(sr => sr.WorkloadId == depId);
                if (depSchedule == null)
                {
                    // Dependency not scheduled yet: treat as not available (shouldn't happen if inputs are topologically ordered)
                    throw new InvalidOperationException($"Workload {workload.Id} depends on {depId} which is not scheduled yet.");
                }
                depEnds.Add(depSchedule.End);
            }

            if (depEnds.Count > 0)
            {
                var maxDepEnd = depEnds.Max();
                if (maxDepEnd.Date > earliest) earliest = maxDepEnd.Date;
                // we could also preserve time (start at the exact dep end), but for day-block scheduling we align by date.
            }

            return earliest;
        }

        /// <summary>
        /// Searches for a consecutive list of ResourceAvailability entries (for a single resource)
        /// starting on or after earliestDate such that:
        ///  - sum(RemainingMinutes) across consecutive days >= workload.DurationInMinutes
        ///  - for each day: RemainingArea >= workload.AreaRequired
        ///  - if workload.IsExclusive == true, each day must have no prior bookings (RemainingMinutes == TotalMinutes)
        ///  - resource lift capacity must already have been checked by caller
        /// Returns the list of ResourceAvailability objects (references to the same objects in the resourceAvailabilities list).
        /// </summary>
        public static List<ResourceAvailability> FindConsecutiveAvailabilityBlockForWorkload(
            Workload workload,
            Resource resource,
            List<ResourceAvailability> resourceAvailabilities,
            DateTime earliestDate,
            SchedulingOptions options)
        {
            if (workload == null) throw new ArgumentNullException(nameof(workload));
            if (resource == null) throw new ArgumentNullException(nameof(resource));
            if (resourceAvailabilities == null) throw new ArgumentNullException(nameof(resourceAvailabilities));

            // get availabilities for this resource ordered by date (only dates >= earliestDate.Date)
            var availsForResource = resourceAvailabilities
                .Where(a => a.ResourceId == resource.Id && a.Date.Date >= earliestDate.Date)
                .OrderBy(a => a.Date)
                .ToList();

            if (!availsForResource.Any()) return null;

            int requiredMinutes = workload.DurationInMinutes;
            int idx = 0;

            // sliding window of consecutive days
            while (idx < availsForResource.Count)
            {
                var block = new List<ResourceAvailability>();
                int minutesAccum = 0;
                int j = idx;

                // consecutive days detection: ensure avails are day-by-day consecutive
                DateTime expectedDate = availsForResource[j].Date.Date;

                while (j < availsForResource.Count && minutesAccum < requiredMinutes)
                {
                    var a = availsForResource[j];

                    // must be exactly expectedDate (consecutive); if a gap appears, break
                    if (a.Date.Date != expectedDate) break;

                    // resource-level checks:
                    //  - this availability must have enough RemainingArea for the workload
                    if (a.RemainingArea < workload.AreaRequired)
                    {
                        break;
                    }

                    //  - exclusivity check: if IsExclusive, this day must have no prior bookings (i.e., RemainingMinutes == TotalMinutes)
                    if (workload.IsExclusive && a.RemainingMinutes != a.TotalMinutes)
                    {
                        break;
                    }

                    // accumulate minutes
                    if (a.RemainingMinutes > 0)
                    {
                        minutesAccum += a.RemainingMinutes;
                        block.Add(a);
                    }
                    else
                    {
                        // this day has zero remaining minutes, cannot use it; break the consecutive block here
                        break;
                    }

                    // move to next day
                    expectedDate = expectedDate.AddDays(1);
                    j++;
                }

                if (minutesAccum >= requiredMinutes)
                {
                    // Found a block that can satisfy duration and per-day area/exclusivity requirements.
                    // But we also should ensure per-day area summation works when multiple non-exclusive workloads share area:
                    // We already checked each day's RemainingArea >= workload.AreaRequired (we'll subtract AreaRequired when allocating).
                    // Also ensure resource capacity (lift weight) satisfied by caller before invoking this function.
                    return block;
                }

                // move start to next day
                idx++;
            }

            // no block found
            return null;
        }

        public static List<ResourceAvailability> FindContinuousSlot(Workload workload, Resource resource, List<ResourceAvailability> avails, DateTime earliestDate, SchedulingOptions options)
        {
            int requiredMinutes = workload.DurationInMinutes;
            int requiredArea = workload.AreaRequired;
            int requiredWeight = workload.EffectiveWeight;

            List<ResourceAvailability> block = new();
            int accumulatedMinutes = 0;

            avails = avails.OrderBy(a => a.Date > earliestDate).ToList();

            foreach (var day in avails)
            {
                // ❌ Fully booked day → break and restart search
                if (day.Status == 2)
                {
                    block.Clear();
                    accumulatedMinutes = 0;
                    continue;  // restart from next day
                }

                // ✔ If partially/fully available but does not meet workload requirements → break and restart
                bool fitsToday =
                    day.RemainingArea >= requiredArea;
                    //&& day.RemainingMinutes >= requiredMinutes;

                if (!fitsToday)
                {
                    block.Clear();
                    accumulatedMinutes = 0;
                    continue;  // restart from next day
                }

                // ✔ This day can be part of the block
                block.Add(day);
                accumulatedMinutes += day.RemainingMinutes;

                // 🎉 Requirement satisfied
                if (accumulatedMinutes >= requiredMinutes)
                {
                    return block;
                }
            }

            // ❌ No block found
            return new List<ResourceAvailability>();
        }

        /// <summary>
        /// Allocate workload into the provided consecutive block. Mutates ResourceAvailability.RemainingMinutes and RemainingArea.
        /// Produces a ScheduleResult with BookedAvailability entries (one per day in the block).
        /// Start is block.First().Date at 00:00, End is Start + Duration minutes (this may cross days).
        /// </summary>
        public static ScheduleResult AllocateWorkloadOnBlock(
            Workload workload, 
            Resource resource, 
            List<ResourceAvailability> block)
        {
            if (workload == null) throw new ArgumentNullException(nameof(workload));
            if (resource == null) throw new ArgumentNullException(nameof(resource));
            if (block == null || block.Count == 0) throw new ArgumentNullException(nameof(block));

            int remainingDuration = workload.DurationInMinutes;
            var bookings = new List<BookedAvailability>();

            foreach (var a in block)
            {
                if (remainingDuration <= 0) break;

                // minutes we can take from this day
                int take = Math.Min(remainingDuration, a.RemainingMinutes);

                if (take <= 0) continue;

                // For area: we subtract full workload.AreaRequired per day (assuming the workload occupies the same area for the whole day).
                // If you want area to be pro-rated by minutes, change this logic to subtract proportionally.
                if (a.RemainingArea < workload.AreaRequired)
                {
                    // This should not happen because we already validated RemainingArea in block selection
                    throw new InvalidOperationException($"Insufficient area on {a.Date:d} when allocating workload {workload.Id} on resource {resource.Id}.");
                }

                // mutate availability
                //if (a.RemainingArea == workload.AreaRequired)
                //{
                //    a.RemainingMinutes -= take;
                //}
                //a.RemainingArea -= workload.AreaRequired;

                if(a.RemainingArea == workload.AreaRequired)
                {
                    a.RemainingMinutes -= take;
                    a.RemainingArea -= workload.AreaRequired;
                    a.Status = 2; // Fully Booked
                } 
                else
                {
                    //Always RemainingArea > workload.AreaRequired
                    if (a.RemainingMinutes > take) { a.RemainingMinutes -= take; }
                    else { /*a.RemainingMinutes -= take;*/ }

                    a.RemainingArea -= workload.AreaRequired;
                    a.Status = 1; // Fully Booked
                }

                // record booked availability (we only have BookedArea in the model)
                bookings.Add(new BookedAvailability(
                    AvailabilityId: a.Id,
                    Date: a.Date,
                    ResourceId: a.ResourceId,
                    BookedArea: workload.AreaRequired,
                    BookedMinutes: take
                ));

                remainingDuration -= take;
            }

            if (remainingDuration > 0)
            {
                // Shouldn't happen because caller ensured block had enough minutes, but safety check
                throw new InvalidOperationException($"Block allocation for workload {workload.Id} did not provide full duration. {remainingDuration} minutes remaining.");
            }

            // Build start and end DateTimes:
            // Start: first block date at midnight (00:00)
            var start = block.First().Date.Date;
            // End: start + runtime duration
            var end = start.AddMinutes(workload.DurationInMinutes);

            return new ScheduleResult(
                WorkloadId: workload.Id,
                Start: start,
                End: bookings.Last().Date.Date,
                Bookings: bookings
            );
        }

    }
}
