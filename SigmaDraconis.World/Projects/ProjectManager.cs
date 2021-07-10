namespace SigmaDraconis.World.Projects
{
    using System.Collections.Generic;
    using System.Linq;
    using Shared;

    public static class ProjectManager
    {
        private static readonly Dictionary<int, Project> projects = new Dictionary<int, Project>();

        public static void Init()
        {
            projects.Clear();

            // Biology Lab
            AddDefinition(1, 18, SkillType.Botanist, ThingType.Biolab);
            AddDefinition(2, 12, SkillType.Botanist, ThingType.Biolab);
            AddDefinition(3, 12, SkillType.Botanist, ThingType.Biolab);
            AddDefinition(4, 12, SkillType.Botanist, ThingType.Biolab);
            AddDefinition(5, 9, SkillType.Botanist, ThingType.Biolab);
            AddDefinition(6, 4, SkillType.Botanist, ThingType.Biolab);
            AddDefinition(7, 12, SkillType.Botanist, ThingType.Biolab);
            AddDefinition(8, 12, SkillType.Botanist, ThingType.Biolab);
            AddDefinition(9, 9, SkillType.Botanist, ThingType.Biolab);
            AddDefinition(10, 12, SkillType.Botanist, ThingType.Biolab);
            AddDefinition(11, 12, SkillType.Botanist, ThingType.Biolab);
            AddDefinition(12, 12, SkillType.Botanist, ThingType.Biolab);

            // Engineering Lab
            AddDefinition(101, 12, SkillType.Engineer, ThingType.MaterialsLab);
            AddDefinition(102, 18, SkillType.Engineer, ThingType.MaterialsLab);
            AddDefinition(103, 18, SkillType.Engineer, ThingType.MaterialsLab);
            AddDefinition(104, 18, SkillType.Engineer, ThingType.MaterialsLab);
            AddDefinition(105, 24, SkillType.Engineer, ThingType.MaterialsLab);

            // Geology Lab
            AddDefinition(201, 9, SkillType.Geologist, ThingType.GeologyLab);
            AddDefinition(202, 15, SkillType.Geologist, ThingType.GeologyLab);
            AddDefinition(203, 9, SkillType.Geologist, ThingType.GeologyLab);
            AddDefinition(204, 12, SkillType.Geologist, ThingType.GeologyLab);
            AddDefinition(205, 15, SkillType.Geologist, ThingType.GeologyLab);

            projects[1].LockedBuildings.Add(ThingType.AlgaePool);
            for (int i = 2; i <= 5; i++) projects[i].RequiredProjects.Add(1);

            projects[6].LockedBuildings.Add(ThingType.PlanterHydroponics);
            projects[7].RequiredProjects.Add(6);
            projects[8].LockedBuildings.Add(ThingType.CompostFactory);
            projects[8].LockedBuildings.Add(ThingType.PlanterStone);
            projects[9].RequiredProjects.Add(8);
            projects[10].RequiredProjects.Add(8);
            projects[11].RequiredProjects.Add(8);
            projects[11].LockedBuildings.Add(ThingType.KekFactory);
            projects[11].LockedBuildings.Add(ThingType.KekDispenser);
            projects[12].RequiredProjects.Add(11);

            projects[101].LockedBuildings.Add(ThingType.BatteryCellFactory);
            projects[101].LockedBuildings.Add(ThingType.Battery);
            projects[102].LockedBuildings.Add(ThingType.CompositesFactory);
            projects[102].LockedBuildings.Add(ThingType.WindTurbine);
            projects[103].LockedBuildings.Add(ThingType.SolarCellFactory);
            projects[103].LockedBuildings.Add(ThingType.SolarPanelArray);
            projects[104].LockedBuildings.Add(ThingType.FuelFactory);
            projects[104].LockedBuildings.Add(ThingType.HydrogenStorage);
            projects[104].LockedBuildings.Add(ThingType.HydrogenBurner);
            projects[102].RequiredProjects.Add(101);
            projects[103].RequiredProjects.Add(101);
            projects[104].RequiredProjects.Add(101);
            projects[105].RequiredProjects.Add(101);
            projects[105].RequiredProjects.Add(102);
            projects[105].RequiredProjects.Add(103);
            projects[105].RequiredProjects.Add(104);
            projects[105].LockedBuildings.Add(ThingType.LaunchPad);
            projects[105].LockedBuildings.Add(ThingType.RocketGantry);
            projects[105].LockedBuildings.Add(ThingType.Rocket);

            projects[202].RequiredProjects.Add(201);
            projects[204].RequiredProjects.Add(203);
            projects[205].RequiredProjects.Add(203);
            projects[203].LockedBuildings.Add(ThingType.OreScanner);
        }

        public static bool CanDoProject(ThingType labType, int projectId)
        {
            return projectId > 0 && projects.Values.Where(d => d.LabType == labType && !d.IsDone && d.RequiredProjects.All(r => GetDefinition(r)?.IsDone == true)).Any(p => p.Id == projectId);
        }

        public static IEnumerable<Project> GetAvailableProjects(ThingType labType)
        {
            return projects.Values.Where(d => d.LabType == labType && !d.IsDone && d.RequiredProjects.All(r => GetDefinition(r)?.IsDone == true)).OrderBy(p => p.DisplayOrder);
        }

        public static IEnumerable<int> GetCompletedProjects()
        {
            return projects.Values.Where(d => d.IsDone).Select(p => p.Id);
        }

        public static IEnumerable<int> GetIncompleteProjects()
        {
            return projects.Values.Where(d => !d.IsDone).Select(p => p.Id);
        }

        public static Project GetDefinition(int id)
        {
            return projects.ContainsKey(id) ? projects[id] : null;
        }

        public static IEnumerable<Project> LockingProjects(ThingType buildingType)
        {
            return projects.Values.Where(d => !d.IsDone && d.LockedBuildings.Any(b => b == buildingType));
        }

        public static IEnumerable<Project> LockingProjects(int projectId)
        {
            return projects[projectId].RequiredProjects.Select(p => GetDefinition(p)).Where(r => !r.IsDone);
        }

        // For serialization
        public static Dictionary<int, int> GetProjectsRemainingWork()
        {
            var kvs = new Dictionary<int, int>();
            foreach (var definition in projects)
            {
                kvs.Add(definition.Key, definition.Value.RemainingWork);
            }

            return kvs;
        }

        // For deserialization
        public static void SetProjectsRemainingWork(Dictionary<int, int> kvs)
        {
            foreach (var kv in kvs)
            {
                if (projects.ContainsKey(kv.Key)) projects[kv.Key].RemainingWork = kv.Value;
            }
        }

        private static void AddDefinition(int id, int workHours, SkillType skillType, ThingType labType)
        {
            projects.Add(id, new Project
            {
                Id = id,
                DisplayOrder = id,
                TotalWork = workHours * 3600,
                RemainingWork = workHours * 3600,
                LabType = labType,
                SkillType = skillType
            });
        }
    }
}
