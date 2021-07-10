namespace SigmaDraconis.Config
{
    using System.Collections.Generic;
    using Draconis.Shared;
    using Shared;

    public class ThingTypeDefinition
    {
        public bool IsPlant { get; set; }
        public BuildingLayer BuildingLayer { get; set; }
        public bool IsNameable { get; set; }
        public int ConstructionTimeMinutes { get; set; } = 10;
        public Energy EnergyCost { get; set; }
        public Dictionary<ItemType, int> ConstructionCosts { get; } = new Dictionary<ItemType, int>();
        public TileBlockModel TileBlockModel { get; set; }
        public int FoundationsRequired { get; set; }
        public int CoastTilesRequired { get; set; }
        public int AdjacentCoastTilesRequired { get; set; }
        public bool CanRecycle { get; set; } = true;
        public bool CanBeIndoor { get; set; } = true;
        public bool CanBeOutdoor { get; set; } = true;
        public bool BlocksConstruction { get; set; } = true;  // Prevent other things from being built in the same tile
        public bool OptionalFoundation { get; set; }
        public int WindBlockFactor { get; set; }
        public List<RendererType> RendererTypes { get; set; } = new List<RendererType>();
        public List<int> RendererLayers { get; set; } = new List<int>();
        public Vector2i Size { get; set; }
        public bool CanBuild { get; set; }
        public bool CanRotate { get; set; }
        public bool AutoRotate { get; set; }   // E.g. walls
        public int? CropDefinitionId { get; set; }
        public ThingType ConduitType { get; set; }
        public int FramesToBreak { get; set; }
        public int MinTemperature { get; set; }
        public string SoundFileName { get; set; }
        public float SoundVolume { get; set; }
        public float SoundFade { get; set; }
        public Direction DefaultBuildDirection { get; set; }
        public bool IsAdjacentBuildAllowed { get; set; } = true;
        public bool IsBuildByCoastAllowed { get; set; } = true;
        public bool IsLaunchPadRequired { get; set; }
        public bool IsRocketGantryRequired { get; set; }
        public bool IsCookerRequired { get; set; }
        public bool IsTableRequired { get; set; }
    }
}