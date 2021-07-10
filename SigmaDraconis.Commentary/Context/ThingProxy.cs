namespace SigmaDraconis.Commentary.Context
{
    using Shared;
    using WorldInterfaces;

    internal class ThingProxy
    {
        public int Id { get; }
        public ThingType ThingType { get; }

        // Buildings
        public bool IsReady { get; }
        public FactoryStatus? FactoryStatus { get; }

        // Plants
        public bool IsFlowering { get; }
        public bool IsFruiting { get; }
        public bool HasUnripeFruit { get; }

        // Rocks
        public ItemType ResourceType { get; set; }

        public ThingProxy(IThing thing)
        {
            this.Id = thing.Id;
            this.ThingType = thing.ThingType;
            this.IsReady = (thing as IBuildableThing)?.IsReady == true;
            this.FactoryStatus = (thing as IFactoryBuilding)?.FactoryStatus;
            this.IsFlowering = (thing as IPlant)?.IsFlowering == true;
            this.IsFruiting = (thing as IFruitPlant)?.CountFruitAvailable > 0 == true;
            this.HasUnripeFruit = (thing as IFruitPlant)?.HasFruitUnripe == true;
            this.ResourceType = (thing as IRock)?.ResourceType ?? ItemType.None;
        }
    }
}
