namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class CharcoalMaker : FactoryBuilding, IResourceProviderBuilding, IResourceConsumerBuilding
    {
        public CharcoalMaker() : base()
        {
        }

        public CharcoalMaker(ISmallTile tile) : base(ThingType.CharcoalMaker, tile, 1)
        {
        }

        protected override void Init()
        {
            this.framesToInitialise = Constants.CharcoalMakerFramesToInitialise;
            this.framesToProcess = Constants.CharcoalMakerFramesToProcess;
            this.framesToPauseResume = Constants.CharcoalMakerFramesToPauseResume;
            this.consumedItemType = ItemType.Biomass;
            this.producedItemType = ItemType.Coal;
            base.Init();
        }
    }
}
