namespace SigmaDraconis.Config
{
    using Shared;

    public class CropDefinition
    {
        public int Id { get; set; }
        public int AnimationStartFrame { get; set; }
        public bool IsCrop { get; set; }
        public string DisplayName { get; set; }
        public string DisplayNameLong { get; set; }
        public string DisplayNameLower { get; set; }
        public int HoursToGrow { get; set; }
        public int MinTemp { get; set; }
        public int MinGoodTemp { get; set; }
        public int MaxGoodTemp { get; set; }
        public int MaxTemp { get; set; }
        public int HarvestYield { get; set; }
        public bool CanGrowHydroponics { get; set; }
        public bool CanGrowSoil { get; set; }
        public int IconIndex { get; set; }
        public int TextR { get; set; }
        public int TextG { get; set; }
        public int TextB { get; set; }
        public ThingType CookerType { get; set; }
        public bool CanEat { get; set; }
        public bool IsWildFruit { get; set; }
    }
}