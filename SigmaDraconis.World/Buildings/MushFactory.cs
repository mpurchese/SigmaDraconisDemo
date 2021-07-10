namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class MushFactory : FactoryBuilding, IEnergyConsumer, IResourceProviderBuilding, IResourceConsumerBuilding, IRepairableThing
    {
        private int animationTimer;

        [ProtoMember(1)]
        public bool HasWater { get; private set; }

        public MushFactory() : base()
        {
        }

        public MushFactory(ISmallTile mainTile) : base(ThingType.MushFactory, mainTile, 1)
        {
        }

        protected override void Init()
        {
            this.framesToInitialise = (int)(3600 * Constants.MushFactoryEnergyStore / Constants.MushFactoryEnergyUse);
            this.framesToProcess = Constants.MushFactoryFramesToProcess;
            this.framesToPauseResume = Constants.MushFactoryFramesToPauseResume;
            this.energyPerHour = Energy.FromKwH(Constants.MushFactoryEnergyUse);
            this.energyPerFrame = energyPerHour / Constants.FramesPerHour;
            this.capacitorSize = Constants.MushFactoryEnergyStore;
            this.consumedItemType = ItemType.Biomass;
            this.producedItemType = ItemType.Mush;
            base.Init();
        }

        protected override void UpdateAnimationFrame()
        {
            switch (this.FactoryStatus)
            {
                case FactoryStatus.Offline:
                    this.AnimationFrame = 1;
                    break;
                case FactoryStatus.Standby:
                    this.AnimationFrame = 2;
                    break;
                case FactoryStatus.NoPower:
                case FactoryStatus.NoResource:
                    this.AnimationFrame = 3;
                    break;
                case FactoryStatus.WaitingToDistribute:
                    this.AnimationFrame = 4;
                    break;
                case FactoryStatus.Broken:
                    this.AnimationFrame = 5;
                    break;
                case FactoryStatus.InProgress:
                    if (this.AnimationFrame < 6) this.AnimationFrame = 6;
                    else if (this.animationTimer >= 1)
                    {
                        this.animationTimer = 0;
                        if (this.animationFrame != 6 || this.FactoryProgress < 0.98f) this.AnimationFrame = this.AnimationFrame < 37 ? this.AnimationFrame + 1 : 6;
                    }
                    else this.animationTimer++;
                    break;
            }
        }

        protected override void UpdateFactoryNoPower()
        {
            if (this.pauseResumeFrameCounter > 0 && this.FactoryProgress > 0 && this.CapacitorCharge > 0)
            {
                this.pauseResumeFrameCounter--;
                if (this.pauseResumeFrameCounter <= 0) this.FactoryStatus = FactoryStatus.Paused;
                else
                {
                    var rate = this.pauseResumeFrameCounter / (double)this.framesToPauseResume;
                    this.CapacitorCharge -= rate * this.energyPerFrame.KWh;
                    this.Process(rate);
                }
            }
            if (World.ResourceNetwork?.CanTakeEnergy(Energy.FromKwH(this.capacitorSize)) == true) this.FactoryStatus = this.HasWater ? FactoryStatus.InProgress : FactoryStatus.NoResource;
        }

        protected override void TryStart()
        {
            var network = World.ResourceNetwork;
            if (network == null) return;

            this.FactoryProgress = 0.0;

            if (!this.CheckTemperatureOK()) this.FactoryStatus = FactoryStatus.TooCold;
            else if (this.InventoryTarget.HasValue && network.GetItemTotal(this.producedItemType) >= this.InventoryTarget.Value) this.FactoryStatus = FactoryStatus.Standby;
            else if (!this.HasWater && !network.CanTakeItems(this, ItemType.Water, Constants.MushFactoryWaterUse)) this.FactoryStatus = FactoryStatus.NoResource;
            else if (this.InputItemType == this.consumedItemType || network.CanTakeItems(this, this.consumedItemType, 1))
            {
                if (this.CapacitorCharge >= this.capacitorSize || network.CanTakeEnergy(Energy.FromKwH(this.capacitorSize - this.CapacitorCharge)))
                {
                    if (this.InputItemType != this.consumedItemType) network.TakeItems(this, this.consumedItemType, 1);
                    if (!this.HasWater)
                    {
                        network.TakeItems(this, ItemType.Water, Constants.MushFactoryWaterUse);
                        this.HasWater = true;
                    }

                    this.FactoryStatus = FactoryStatus.InProgress;
                    this.InputItemType = ItemType.None;
                }
                else this.FactoryStatus = FactoryStatus.NoPower;
            }
            else
            {
                this.FactoryStatus = FactoryStatus.Standby;
            }
        }

        protected override void CompleteProcessing()
        {
            this.HasWater = false;
            base.CompleteProcessing();
        }

        protected override void TryDistribute()
        {
            var itemCount = this.OutputItemCount;
            base.TryDistribute();
            if (this.OutputItemCount < itemCount) WorldStats.Increment(WorldStatKeys.MushChurned, itemCount - this.OutputItemCount);
        }
    }
}
