using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingLotAlgorithm
{
    #region Models
    public enum StageCode { FinalAssembly, SubAssembly, Fabrication }
    public enum TaskStatus { NotStarted, InProgress, Done }


    public class Shop
    {
        public string ShopId { get; set; }
        public string ShopName { get; set; }
        public int TotalAreaSqM { get; set; }
        public bool IsCcr { get; set; } = true;
        public int ShopPriority { get; set; }
        public List<Zone> Zones { get; set; } = new();
    }

    public class Zone
    {
        public string ZoneId { get; set; }
        public string ShopId { get; set; }
        public double EffectiveLiftTonnes { get; set; } // null means no lift (set to 0)
        public int ZoneAreaSqM { get; set; }
    }

    public class TaskItem
    {
        public string TaskId { get; set; }
        public string ProjectId { get; set; }
        public StageCode Stage { get; set; }
        public double WeightToLiftT { get; set; }
        public int AreaRequired { get; set; }
        public int DurationDays { get; set; }
        public DateTime Pdd { get; set; }
        public TaskStatus Status { get; set; }
    }

    public class Allocation
    {
        public string TaskId { get; set; }
        public string ShopId { get; set; }
        public string ZoneId { get; set; }
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        public bool CrossShopFlag { get; set; }
    }

    public class UtilizationRecord
    {
        public string ZoneId { get; set; }
        public DateTime Period { get; set; }
        public int AreaLoaded { get; set; }
        public double LiftLoaded { get; set; }
        public int AvailableArea { get; set; }
    }

    public class Project
    {
        public string ProjectId { get; set; }
        public int PortfolioPriority { get; set; }
        public List<TaskItem> Tasks { get; set; } = new();
    }

    public class Slot
    {
        // Core assignment
        public TaskItem Task { get; set; }
        public Zone Zone { get; set; }

        // Time window
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }

        // Feasibility metrics
        public double RemainingArea { get; set; }        // min remaining area over the slot days
        public double RemainingLift { get; set; }        // optional lift remaining (if modeled per-day)
        public int CrossShopPenalty { get; set; }        // 0 = same shop, 1 = different shop

        // Optional evaluation weights (AI/score extension)
        public double Score { get; set; }                // computed later by strategy

        // Metadata to explain decision
        public string Reason { get; set; }               // for debug: "Earliest-Fit", "MinCrossShop", etc.

        // Helper
        public override string ToString()
        {
            return $"{Task.TaskId} => {Zone.ShopId}-{Zone.ZoneId} | {Start:dd-MMM} to {Finish:dd-MMM} | Score={Score:F2}";
        }
    }

    #endregion
}
