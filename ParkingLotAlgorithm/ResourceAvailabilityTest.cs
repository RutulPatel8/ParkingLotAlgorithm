// ----------------------------
// INPUT
// ----------------------------
using ParkingLotAlgorithm;

public class ResourceAvailabilityTest
{
    public ResourceAvailabilityTest(int Id, int TotalMinutes)
    {
        this.Id = Id;
        this.TotalMinutes = TotalMinutes;
    }

    public int Id { get; }

    public readonly int TotalMinutes;
}