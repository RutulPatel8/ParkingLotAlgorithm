using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingLotAlgorithm
{
    #region Scheduler Helpers
    // Per-zone daily capacity tracker (simple day-granularity horizon)
    public class ZoneCalendar
    {
        public Zone Zone { get; set; }
        public Dictionary<DateTime, DayLoad> Loads { get; set; } = new();
        public int CapacityArea => Zone.ZoneAreaSqM;


        public ZoneCalendar(Zone z)
        {
            Zone = z;
        }


        public DayLoad GetLoadFor(DateTime day)
        {
            if (!Loads.ContainsKey(day)) Loads[day] = new DayLoad();
            return Loads[day];
        }


        public bool CanFit(DateTime start, int duration, int areaReq, double liftReq)
        {
            for (int i = 0; i < duration; i++)
            {
                var d = start.Date.AddDays(i);
                var load = GetLoadFor(d);
                if (load.Area + areaReq > CapacityArea) return false;
                // lift feasibility: zone's effective lift must be >= liftReq
                if (Zone.EffectiveLiftTonnes < liftReq) return false;
            }
            return true;
        }


        public void Reserve(DateTime start, int duration, int areaReq, double liftReq)
        {
            for (int i = 0; i < duration; i++)
            {
                var d = start.Date.AddDays(i);
                var load = GetLoadFor(d);
                load.Area += areaReq;
                load.Lift = Math.Max(load.Lift, liftReq); // conservative
            }
        }
    }


    public class DayLoad
    {
        public int Area { get; set; } = 0;
        public double Lift { get; set; } = 0.0;
    }


    public record SchedulingResult(List<Allocation> Allocations, List<UtilizationRecord> Utilization, Dictionary<string, double> Metrics);
    #endregion
}
