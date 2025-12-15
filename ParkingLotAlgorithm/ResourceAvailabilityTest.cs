// ----------------------------
// INPUT
// ----------------------------
using ParkingLotAlgorithm;

public class ResourceAvailabilityTest
{
    public ResourceAvailabilityTest(int resourceId, int date, int totalMinutes, int effectiveLiftTonnes)
    {
        ResourceId = resourceId;
        Date = date;
        TotalMinutes = totalMinutes;
        EffectiveLiftTonnes = effectiveLiftTonnes;
    }

    public int ResourceId { get; }
    public int Date { get; }          // e.g. 20240101 or DayIndex
    public int TotalMinutes { get; }
    public int EffectiveLiftTonnes { get; }
}
