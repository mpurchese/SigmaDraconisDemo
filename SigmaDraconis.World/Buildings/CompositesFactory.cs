namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class CompositesFactory : FactoryBuilding, IEnergyConsumer, IResourceProviderBuilding, IResourceConsumerBuilding, IRepairableThing, IRotatableThing
    {
        private int animationTimer;

        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        [ProtoMember(2)]
        public bool HasOrganics { get; private set; }

        [ProtoMember(3)]
        public bool HasMetal { get; private set; }

        public CompositesFactory() : base()
        {
        }

        public CompositesFactory(ISmallTile mainTile, Direction direction) : base(ThingType.CompositesFactory, mainTile, 2)
        {
            this.Direction = direction;
        }

        protected override void Init()
        {
            this.framesToInitialise = (int)(3600 * Constants.CompositesFactoryEnergyStore / Constants.CompositesFactoryEnergyUse);
            this.framesToProcess = Constants.CompositesFactoryFramesToProcess;
            this.framesToPauseResume = Constants.CompositesFactoryFramesToPauseResume;
            this.energyPerHour = Energy.FromKwH(Constants.CompositesFactoryEnergyUse);
            this.energyPerFrame = energyPerHour / Constants.FramesPerHour;
            this.capacitorSize = Constants.CompositesFactoryEnergyStore;
            this.producedItemType = ItemType.Composites;
            base.Init();
        }

        protected override void CompleteProcessing()
        {
            this.FactoryProgress = 1.0;
            this.pauseResumeFrameCounter = 0;
            this.FactoryStatus = FactoryStatus.WaitingToDistribute;
            this.InputItemType = ItemType.None;
            this.OutputItemType = ItemType.Composites;
            this.OutputItemCount = 4;
            this.TryDistribute();
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{base.GetTextureName()}_{this.Direction.ToString()}";
        }

        public override bool CanAddInput(ItemType itemType)
        {
            var network = World.ResourceNetwork;
            if (network == null) return false;

            if (this.FactoryStatus != FactoryStatus.Standby || !this.IsSwitchedOn) return false;
            if (this.InventoryTarget.HasValue && network.GetItemTotal(this.producedItemType) >= this.InventoryTarget.Value) return false;
            if (itemType == ItemType.Biomass && !this.HasOrganics && (this.HasMetal || network.CanTakeItems(this, ItemType.Metal, 1))) return true;
            if (itemType == ItemType.Metal && !this.HasMetal && (this.HasOrganics || network.CanTakeItems(this, ItemType.Biomass, 1))) return true;

            return false;
        }

        public override void AddInput(ItemType itemType)
        {
            var network = World.ResourceNetwork;
            if (network == null) return;

            if (itemType == ItemType.Metal && !this.HasOrganics) network.TakeItems(this, ItemType.Biomass, 1);
            else if (itemType == ItemType.Biomass && !this.HasMetal) network.TakeItems(this, ItemType.Metal, 1);

            this.HasMetal = true;
            this.HasOrganics = true;
        }

        protected override void TryStart()
        {
            var network = World.ResourceNetwork;
            if (network == null) return;

            this.FactoryProgress = 0.0;

            if (this.InventoryTarget.HasValue && network.GetItemTotal(this.producedItemType) >= this.InventoryTarget.Value)
            {
                this.FactoryStatus = FactoryStatus.Standby;
                return;
            }

            var metalAvailable = this.HasMetal || network.CanTakeItems(this, ItemType.Metal, 1);
            var organicsAvailable = this.HasOrganics || network.CanTakeItems(this, ItemType.Biomass, 1);

            if (metalAvailable && organicsAvailable)
            {
                if (this.CapacitorCharge >= this.capacitorSize || network.CanTakeEnergy(Energy.FromKwH(this.capacitorSize - this.CapacitorCharge)))
                {
                    if (!this.HasMetal) network.TakeItems(this, ItemType.Metal, 1);
                    if (!this.HasOrganics) network.TakeItems(this, ItemType.Biomass, 1);
                    this.FactoryStatus = FactoryStatus.InProgress;
                    this.HasMetal = false;
                    this.HasOrganics = false;
                }
                else this.FactoryStatus = FactoryStatus.NoPower;
            }
            else
            {
                this.FactoryStatus = FactoryStatus.Standby;
            }
        }

        protected override void Process(double rate)
        {
            base.Process(rate);
            this.smokeSoundRate = (this.animationFrame - 4) / 16f;
        }

        protected override void UpdateAnimationFrame()
        {
            if (this.animationTimer > 0)
            {
                this.animationTimer--;
                return;
            }

            this.animationTimer = 2;

            switch (this.FactoryStatus)
            {
                case FactoryStatus.InProgress:
                    this.AnimationFrame = this.animationFrame < 12 ? this.animationFrame + 1 : 4;
                    this.smokeSoundRate = 1f;
                    break;
                case FactoryStatus.Stopping:
                case FactoryStatus.Pausing:
                case FactoryStatus.Initialising:
                case FactoryStatus.Starting:
                case FactoryStatus.Resuming:
                case FactoryStatus.Standby:
                case FactoryStatus.Paused:
                    this.AnimationFrame = 3;
                    this.smokeSoundRate = 0;
                    break;
                case FactoryStatus.Broken:
                case FactoryStatus.WaitingToDistribute:
                    this.AnimationFrame = 2;
                    this.smokeSoundRate = 0;
                    break;
                default:
                    this.AnimationFrame = 1;
                    this.smokeSoundRate = 0;
                    break;
            }
        }

        protected override void BeforeRecycle()
        {
            this.AnimationFrame = 1;
            base.BeforeRecycle();
        }
    }
}
