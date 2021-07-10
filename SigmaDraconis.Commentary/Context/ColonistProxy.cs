namespace SigmaDraconis.Commentary.Context
{
    using System.Collections.Generic;
    using System.Linq;
    using Shared;
    using Cards.Interface;
    using Config;
    using WorldInterfaces;

    internal class ColonistProxy
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public SkillType SkillType { get; set; }
        public bool IsArrived { get; set; }
        public List<CardType> Cards { get; } = new List<CardType>();
        public List<int> LikedFoodIds { get; } = new List<int>();
        public List<int> DislikedFoodIds { get; } = new List<int>();
        public double Energy { get; set; }
        public bool IsSleeping { get; set; }
        public bool IsWorking { get; set; }
        public bool IsIdle { get; set; }

        public int LastLabProjectId { get; set; }
        public List<ThingProxy> AdjacentThings { get; } = new List<ThingProxy>();
        public ColonistActivityType ActivityType { get; }
        public Dictionary<int, int> TimeSinceSocialByColonist { get; } = new Dictionary<int, int>();
        public bool IsByCoast { get; }
        public bool IsIndoors { get; }
        public bool IsInSleepPod { get; }
        public string LastFoodType { get; }
        public ItemType LastResourceFound { get; set; }
        public MineResourceDensity LastResourceDensityFound { get; set; }
        public ItemType ScannerResourceFound { get; set; }
        public MineResourceDensity ScannerResourceDensityFound { get; set; }

        public int StorySport { get; }
        public int StoryInstrument { get; }
        public int StoryWorkedplace { get; }

        public ColonistProxy(IColonist colonist)
        {
            this.Id = colonist.Id;
            this.Name = colonist.ShortName;
            this.SkillType = colonist.Skill;
            this.IsArrived = colonist.IsArrived;
            this.Cards.AddRange(colonist.Cards.Cards.Select(d => d.Key));
            this.Energy = colonist.Body.Energy;
            this.IsSleeping = colonist.Body.IsSleeping;
            this.IsWorking = colonist.IsWorking;
            this.IsIdle = colonist.IsIdle && colonist.IdleTimer >= 120;
            this.LastLabProjectId = colonist.LastLabProjectId;
            this.LikedFoodIds.AddRange(colonist.GetFoodLikes().Select(f => f.Id));
            this.DislikedFoodIds.AddRange(colonist.GetFoodLikes().Select(f => f.Id));
            this.ActivityType = colonist.ActivityType;
            this.TimeSinceSocialByColonist = colonist.TimeSinceSocialByColonist.ToDictionary(kv => kv.Key, kv => kv.Value);
            this.IsByCoast = colonist.MainTile.AdjacentTiles8.Any(t => t.TerrainType == TerrainType.Coast);
            this.IsIndoors = colonist.MainTile.ThingsAll.Any(t => t.ThingType == ThingType.Roof);
            this.IsInSleepPod = colonist.MainTile.ThingsAll.Any(t => t.ThingType == ThingType.SleepPod);
            this.LastFoodType = CropDefinitionManager.GetDefinition(colonist.LastFoodType)?.DisplayName ?? "";
            this.LastResourceFound = colonist.LastResourceFound;
            this.LastResourceDensityFound = colonist.LastResourceDensityFound;
            this.ScannerResourceFound = colonist.ScannerResourceFound;
            this.ScannerResourceDensityFound = colonist.ScannerResourceDensityFound;
            this.StorySport = colonist.StorySport;
            this.StoryInstrument = colonist.StoryInstrument;
            this.StoryWorkedplace = colonist.StoryWorkedplace;
        }
    }
}
