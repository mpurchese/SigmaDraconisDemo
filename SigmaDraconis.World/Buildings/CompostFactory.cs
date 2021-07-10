namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using Projects;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class CompostFactory : FactoryBuilding, IEnergyConsumer, IResourceProviderBuilding, IResourceConsumerBuilding, IRepairableThing, IRotatableThing, ICompostFactory
    {
        private int animationTimer;

        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        [ProtoMember(2, IsRequired = true)]
        public bool AllowMush { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public bool AllowOrganics { get; set; }

        public CompostFactory() : base()
        {
        }

        public CompostFactory(ISmallTile mainTile, Direction direction) : base(ThingType.CompostFactory, mainTile, 1)
        {
            this.Direction = direction;
        }

        protected override void Init()
        {
            this.framesToInitialise = (int)(3600 * Constants.CompostFactoryEnergyStore / Constants.CompostFactoryEnergyUse);
            this.framesToProcess = Constants.CompostFactoryFramesToProcess;
            this.framesToPauseResume = Constants.CompostFactoryFramesToPauseResume;
            this.energyPerHour = Energy.FromKwH(Constants.CompostFactoryEnergyUse);
            this.energyPerFrame = energyPerHour / Constants.FramesPerHour;
            this.capacitorSize = Constants.CompostFactoryEnergyStore;
            this.producedItemType = ItemType.Compost;
            base.Init();
        }

        public override void AfterConstructionComplete()
        {
            base.AfterConstructionComplete();
            this.AllowOrganics = true;
        }

        protected override void Process(double rate)
        {
            this.framesToProcess = ProjectManager.GetDefinition(9)?.IsDone == true ? Constants.CompostFactoryFramesToProcessImproved : Constants.CompostFactoryFramesToProcess;
            base.Process(rate);
        }

        protected override void CompleteProcessing()
        {
            this.FactoryProgress = 1.0;
            this.pauseResumeFrameCounter = 0;
            this.FactoryStatus = FactoryStatus.WaitingToDistribute;
            this.InputItemType = ItemType.None;
            this.OutputItemType = ItemType.Compost;
            this.OutputItemCount = 1;
            this.TryDistribute();
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{base.GetTextureName()}_{this.Direction}";
        }

        public override bool CanAddInput(ItemType itemType)
        {
            if (!this.IsSwitchedOn || this.FactoryStatus != FactoryStatus.Standby || this.InputItemType != ItemType.None) return false;
            if (!(this.AllowOrganics && itemType == ItemType.Biomass) && !(this.AllowMush && itemType == ItemType.Mush)) return false;
            if (this.InventoryTarget.HasValue && World.ResourceNetwork?.GetItemTotal(this.producedItemType) >= this.InventoryTarget.Value) return false;
            return true;
        }

        protected override void TryStart()
        {
            var network = World.ResourceNetwork;
            if (network == null) return;

            this.FactoryProgress = 0.0;

            if (this.CapacitorCharge < this.capacitorSize && !network.CanTakeEnergy(Energy.FromKwH(this.capacitorSize - this.CapacitorCharge)))
            {
                this.FactoryStatus = FactoryStatus.NoPower;
                return;
            }

            if (this.InventoryTarget.HasValue && network.GetItemTotal(this.producedItemType) >= this.InventoryTarget.Value)
            {
                this.FactoryStatus = FactoryStatus.Standby;
                return;
            }

            if (this.InputItemType == ItemType.None && this.AllowOrganics && network.CanTakeItems(this, ItemType.Biomass, 1))
            {
                network.TakeItems(this, ItemType.Biomass, 1);
                this.InputItemType = ItemType.Biomass;
            }
            else if (this.InputItemType == ItemType.None && this.AllowMush && network.CanTakeItems(this, ItemType.Mush, 1))
            {
                network.TakeItems(this, ItemType.Mush, 1);
                this.InputItemType = ItemType.Mush;
            }

            if (this.InputItemType == ItemType.None)
            {
                this.FactoryStatus = FactoryStatus.Standby;
                return;
            }

            this.FactoryStatus = FactoryStatus.InProgress;
            this.InputItemType = ItemType.None;
        }


        protected override void UpdateAnimationFrame()
        {
            if (this.animationTimer > 0)
            {
                this.animationTimer--;
                return;
            }

            this.animationTimer = 1;
            var frame = this.animationFrame;

            switch (this.FactoryStatus)
            {
                case FactoryStatus.InProgress:
                    frame = frame >= 20 ? 4 : frame + 1;
                    break;
                case FactoryStatus.Stopping:
                case FactoryStatus.Pausing:
                case FactoryStatus.Initialising:
                case FactoryStatus.Starting:
                case FactoryStatus.Resuming:
                case FactoryStatus.Standby:
                case FactoryStatus.Paused:
                    frame = 3;
                    break;
                case FactoryStatus.Broken:
                    frame = 2;
                    break;
                default:
                    frame = 1;
                    break;
            }

            this.AnimationFrame = frame;
        }

        protected override void BeforeRecycle()
        {
            this.AnimationFrame = 1;
            base.BeforeRecycle();
        }
    }
}
