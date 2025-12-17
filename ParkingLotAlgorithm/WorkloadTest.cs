// ----------------------------
// INPUT
// ----------------------------
public class WorkloadTest
{
    public int id;
    public int durationInMinutes;
    public int weightToLiftTonnes;
    public int unitRequired;

    public WorkloadTest(int Id, int DurationInMinutes, int WeightToLiftTonnes, int unitRequired)
    {
        id = Id;
        durationInMinutes = DurationInMinutes;
        this.weightToLiftTonnes = WeightToLiftTonnes;
        this.unitRequired = unitRequired;
    }
}