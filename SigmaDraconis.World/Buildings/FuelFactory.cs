namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class FuelFactory : FactoryBuilding, IEnergyConsumer, IRotatableThing, IRepairableThing, IResourceProviderBuilding
    {
        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        public FuelFactory() : base()
        {
            this.energyPerHour = Energy.FromKwH(Constants.FuelFactoryEnergyUsage);
            this.energyPerFrame = this.energyPerHour / Constants.FramesPerHour;
        }

        public FuelFactory(ISmallTile tile, Direction direction) : base(ThingType.FuelFactory, tile, 2)
        {
            this.Direction = direction;
            this.energyPerHour = Energy.FromKwH(Constants.FuelFactoryEnergyUsage);
            this.energyPerFrame = this.energyPerHour / Constants.FramesPerHour;
        }

        protected override void Init()
        {
            this.framesToInitialise = (int)(3600 * Constants.FuelFactoryEnergyStore / Constants.FuelFactoryEnergyUse);
            this.framesToPauseResume = Constants.FuelFactoryFramesToPauseResume;
            this.framesToProcess = Constants.FuelFactoryFramesToProcess;
            this.energyPerHour = Energy.FromKwH(Constants.FuelFactoryEnergyUse);
            this.energyPerFrame = energyPerHour / Constants.FramesPerHour;
            this.capacitorSize = Constants.FuelFactoryEnergyStore;
            this.consumedItemType = ItemType.None;
            this.producedItemType = ItemType.LiquidFuel;
            base.Init();
        }

        protected override void UpdateAnimationFrame()
        {
            if (this.FactoryStatus == FactoryStatus.InProgress) this.AnimationFrame = 2;
            else if (this.FactoryStatus == FactoryStatus.WaitingToDistribute || (this.FactoryStatus == FactoryStatus.Broken && World.WorldTime.Minute % 2 == 0)) this.AnimationFrame = 3;
            else this.AnimationFrame = 1;
        }

        public override string GetTextureName(int layer = 1)
        {
            return layer == 1 ? $"{base.GetTextureName()}_{this.Direction.ToString()}" : $"FuelFactoryPipes_{this.Direction.ToString()}";
        }

        public override int GetAnimationFrameForDeconstructOverlay()
        {
            return this.AnimationFrame;
        }
    }
}
