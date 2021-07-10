namespace SigmaDraconis.CheckList.Context
{
    using Shared;
    using WorldInterfaces;

    internal class ThingProxy
    {
        public int Id { get; }
        public ThingType ThingType { get; }
        public bool IsReady { get; }
        public bool InProgress { get; }

        public ThingProxy(IThing thing)
        {
            this.Id = thing.Id;
            this.ThingType = thing.ThingType;
            this.IsReady = (thing as IBuildableThing)?.IsReady == true;
            this.InProgress = (thing as IFactoryBuilding)?.FactoryStatus == FactoryStatus.InProgress;
        }
    }
}
