namespace ParkingLotAlgorithm
{
    public static class DemoDataGenerator
    {
        public static (List<Shop> shops, List<Project> projects) GenerateSampleDataset()
        {
            var shops = new List<Shop>{ new Shop { ShopId = "PV-01", ShopName = "PV-01", TotalAreaSqM = 900, ShopPriority = 1, Zones = new List<Zone>{
                                        new Zone { ZoneId = "PV-01-Z1", ShopId = "PV-01", EffectiveLiftTonnes=40, ZoneAreaSqM=650 },
                                        new Zone { ZoneId = "PV-01-Z2", ShopId = "PV-01", EffectiveLiftTonnes=15, ZoneAreaSqM=150 },
                                        new Zone { ZoneId = "PV-01-Z3", ShopId = "PV-01", EffectiveLiftTonnes=0, ZoneAreaSqM=100 },
                                        }} ,
                                        new Shop { ShopId = "PV-02", ShopName = "PV-02", TotalAreaSqM = 1400, ShopPriority = 2, Zones = new List<Zone>
                                        {
                                        new Zone { ZoneId = "PV-02-Z1", ShopId = "PV-02", EffectiveLiftTonnes=100, ZoneAreaSqM=1000 },
                                        new Zone { ZoneId = "PV-02-Z2", ShopId = "PV-02", EffectiveLiftTonnes=40, ZoneAreaSqM=250 },
                                        new Zone { ZoneId = "PV-02-Z3", ShopId = "PV-02", EffectiveLiftTonnes=0, ZoneAreaSqM=150 },
                                        }},
                                        new Shop { ShopId = "PV-02-EXT", ShopName = "PV-02 Ext", TotalAreaSqM = 1000, ShopPriority = 3, Zones = new List<Zone>
                                        {
                                        new Zone { ZoneId = "PV-02E-Z1", ShopId = "PV-02-EXT", EffectiveLiftTonnes=60, ZoneAreaSqM=700 },
                                        new Zone { ZoneId = "PV-02E-Z2", ShopId = "PV-02-EXT", EffectiveLiftTonnes=40, ZoneAreaSqM=100 },
                                        new Zone { ZoneId = "PV-02E-Z3", ShopId = "PV-02-EXT", EffectiveLiftTonnes=25, ZoneAreaSqM=150 },
                                        new Zone { ZoneId = "PV-02E-Z4", ShopId = "PV-02-EXT", EffectiveLiftTonnes=0, ZoneAreaSqM=50 },
                                        }},
                                        new Shop { ShopId = "PV-03", ShopName = "PV-03", TotalAreaSqM = 1000, ShopPriority = 4, Zones = new List<Zone>
                                        {
                                        new Zone { ZoneId = "PV-03-Z1", ShopId = "PV-03", EffectiveLiftTonnes=25, ZoneAreaSqM=600 },
                                        new Zone { ZoneId = "PV-03-Z2", ShopId = "PV-03", EffectiveLiftTonnes=15, ZoneAreaSqM=250 },
                                        new Zone { ZoneId = "PV-03-Z3", ShopId = "PV-03", EffectiveLiftTonnes=5, ZoneAreaSqM=100 },
                                        new Zone { ZoneId = "PV-03-Z4", ShopId = "PV-03", EffectiveLiftTonnes=0, ZoneAreaSqM=50 },
                                        }},
                                        new Shop { ShopId = "PV-06", ShopName = "PV-06", TotalAreaSqM = 800, ShopPriority = 5, Zones = new List<Zone>
                                        {
                                        new Zone { ZoneId = "PV-06-Z1", ShopId = "PV-06", EffectiveLiftTonnes=20, ZoneAreaSqM=500 },
                                        new Zone { ZoneId = "PV-06-Z2", ShopId = "PV-06", EffectiveLiftTonnes=15, ZoneAreaSqM=220 },
                                        new Zone { ZoneId = "PV-06-Z3", ShopId = "PV-06", EffectiveLiftTonnes=0, ZoneAreaSqM=80 },
                                        }},
                                        new Shop { ShopId = "PV-06-EXT", ShopName = "PV-06 Ext", TotalAreaSqM = 900, ShopPriority = 6, Zones = new List<Zone>
                                        {
                                        new Zone { ZoneId = "PV-06E-Z1", ShopId = "PV-06-EXT", EffectiveLiftTonnes=40, ZoneAreaSqM=600 },
                                        new Zone { ZoneId = "PV-06E-Z2", ShopId = "PV-06-EXT", EffectiveLiftTonnes=15, ZoneAreaSqM=220 },
                                        new Zone { ZoneId = "PV-06E-Z3", ShopId = "PV-06-EXT", EffectiveLiftTonnes=0, ZoneAreaSqM=80 },
                                        }},
                        };


            // generate some projects + tasks (simplified). We'll create 10 projects for demo; user indicated 50 in fuller dataset.
            var projects = new List<Project>();
            var rng = new Random(123);
            DateTime baseDate = new DateTime(2026, 2, 1);


            for (int p = 1; p <= 10; p++)
            {
                var proj = new Project { ProjectId = $"P{p:00}", PortfolioPriority = p };
                // create 3 tasks: FA, Sub, Fab
                var t1 = new TaskItem
                {
                    TaskId = $"P{p:00}-T1",
                    ProjectId = proj.ProjectId,
                    Stage = StageCode.FinalAssembly,
                    WeightToLiftT = rng.Next(5, 100),
                    AreaRequired = rng.Next(50, 700),
                    DurationDays = rng.Next(5, 25),
                    Pdd = baseDate.AddDays(rng.Next(-10, 60)),
                    Status = TaskStatus.NotStarted
                };
                var t2 = new TaskItem
                {
                    TaskId = $"P{p:00}-T2",
                    ProjectId = proj.ProjectId,
                    Stage = StageCode.SubAssembly,
                    WeightToLiftT = Math.Max(1, t1.WeightToLiftT * 0.6),
                    AreaRequired = Math.Max(10, t1.AreaRequired / 3),
                    DurationDays = Math.Max(3, t1.DurationDays / 2),
                    Pdd = t1.Pdd.AddDays(-10),
                    Status = TaskStatus.NotStarted
                };
                var t3 = new TaskItem
                {
                    TaskId = $"P{p:00}-T3",
                    ProjectId = proj.ProjectId,
                    Stage = StageCode.Fabrication,
                    WeightToLiftT = Math.Max(0.5, t2.WeightToLiftT * 0.5),
                    AreaRequired = Math.Max(5, t2.AreaRequired / 2),
                    DurationDays = Math.Max(2, t2.DurationDays / 2),
                    Pdd = t2.Pdd.AddDays(-10),
                    Status = TaskStatus.NotStarted
                };
                proj.Tasks.AddRange(new[] { t1, t2, t3 });
                projects.Add(proj);
            }


            return (shops, projects);
        }
    }

}
