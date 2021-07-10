namespace SigmaDraconis.World.Buildings
{
    using System;
    using ProtoBuf;
    using Language;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class ShorePump : FactoryBuilding, IEnergyConsumer, IWaterProviderBuilding, IRotatableThing, IRepairableThing, ISilo
    {
        private int animationTimer;

        [ProtoMember(1)]
        protected ResourceContainer resourceContainer;

        [ProtoMember(2)]
        public SiloStatus SiloStatus { get; set; }

        [ProtoMember(3)]
        public bool IsSiloSwitchedOn { get; set; }

        [ProtoMember(4)]
        public Direction Direction { get; private set; }

        [ProtoMember(5)]
        public int WaterGenRate { get; private set; }

        [ProtoMember(6)]
        private int networkFullCooldown;

        public int StorageLevel => this.resourceContainer.StorageLevel;
        public int StorageCapacity => this.resourceContainer.StorageCapacity;

        public ShorePump() : base()
        {
        }

        public ShorePump(ISmallTile mainTile, Direction direction) : base(ThingType.ShorePump, mainTile, 1)
        {
            this.Direction = direction;
            this.resourceContainer = new ResourceContainer(Constants.ShorePumpCapacity);
        }

        public override void AfterConstructionComplete()
        {
            this.SiloStatus = SiloStatus.Online;
            this.IsSiloSwitchedOn = true;
            base.AfterConstructionComplete();
        }

        public int CountItems(ItemType itemType)
        {
            return this.resourceContainer.GetItemTotal(itemType);
        }

        public bool CanAddItem(ItemType itemType)
        {
            return itemType == ItemType.Water && this.SiloStatus == SiloStatus.Online && this.resourceContainer.StorageCapacity > this.resourceContainer.StorageLevel;
        }

        public void AddItem(ItemType itemType)
        {
            this.resourceContainer.AddItems(itemType, 1);
        }

        public bool CanTakeItems(ItemType itemType, int count = 1)
        {
            return this.resourceContainer.CanTakeItems(itemType, count);
        }

        public int TakeItems(ItemType itemType, int count = 1)
        {
            return this.resourceContainer.TakeItems(itemType, count);
        }

        public bool SwapItem(ItemType itemTypeToRemove, ItemType itemTypeToAdd)
        {
            return false;
        }

        protected override void Init()
        {
            this.framesToInitialise = (int)(3600 * Constants.ShorePumpEnergyStore / Constants.ShorePumpEnergyUse);
            this.framesToPauseResume = Constants.ShorePumpFramesToPauseResume;
            this.framesToProcess = Constants.ShorePumpFramesToProcess;
            this.energyPerHour = Energy.FromKwH(Constants.ShorePumpEnergyUse);
            this.energyPerFrame = energyPerHour / Constants.FramesPerHour;
            this.capacitorSize = Constants.ShorePumpEnergyStore;
            this.producedItemType = ItemType.Water;
            this.minTemperature = Constants.ShorePumpMinTemperature;
            base.Init();
        }

        public override void Update()
        {
            if (this.IsDesignatedForRecycling) this.IsSiloSwitchedOn = false;

            if (this.IsSiloSwitchedOn) this.SiloStatus = SiloStatus.Online;
            else this.SiloStatus = this.resourceContainer.StorageLevel > 0 ? SiloStatus.WaitingToDistribute : SiloStatus.Offline;

            if (this.networkFullCooldown > 0) this.networkFullCooldown--;

            if (this.SiloStatus == SiloStatus.WaitingToDistribute)
            {
                if (World.ResourceNetwork?.CanAddItem(ItemType.Water, this) == true)
                {
                    World.ResourceNetwork.AddItem(ItemType.Water, false);
                    this.resourceContainer.TakeItems(ItemType.Water, 1);
                }
            }

            base.Update();

            var targetWaterGenRate = this.FactoryStatus == FactoryStatus.InProgress ? Constants.ShorePumpWaterGenRate : 0;
            if (targetWaterGenRate > this.WaterGenRate) this.WaterGenRate = Math.Min(targetWaterGenRate, this.WaterGenRate + 20);
            else if (targetWaterGenRate < this.WaterGenRate) this.WaterGenRate = Math.Max(targetWaterGenRate, this.WaterGenRate - 20);
        }

        protected override void TryStart()
        {
            // Only stop if pump is in standby position
            if ((this.animationFrame <= 3 && this.OutputItemCount > 0) || this.networkFullCooldown > 0)
            {
                this.FactoryStatus = FactoryStatus.WaitingToDistribute;
                return;
            }

            if (this.CapacitorCharge >= this.capacitorSize || World.ResourceNetwork?.CanTakeEnergy(Energy.FromKwH(this.capacitorSize - this.CapacitorCharge)) == true)
            {
                this.FactoryStatus = FactoryStatus.InProgress;
                this.InputItemType = ItemType.None;
            }
            else this.FactoryStatus = FactoryStatus.NoPower;
        }

        protected override void Process(double rate)
        {
            // May have excess output if network was full
            if (this.OutputItemCount > 0 && World.ResourceNetwork?.CanAddItem(ItemType.Water) == true)
            {
                World.ResourceNetwork.AddItem(this.OutputItemType);
                this.OutputItemCount--;
                WorldStats.Increment(WorldStatKeys.WaterPumped);
            }

            // Pause if in standby position and we have excess output
            if (this.animationFrame <= 3 && this.OutputItemCount > 0)
            {
                this.FactoryStatus = FactoryStatus.WaitingToDistribute;
                return;
            }

            base.Process(rate);
        }

        protected override void CompleteProcessing()
        {
            // Don't set output type - this is done by AddWaterFromGround
            this.FactoryProgress = 1.0;
            this.pauseResumeFrameCounter = 0;
            this.FactoryStatus = FactoryStatus.WaitingToDistribute;
            this.OutputItemCount++;
            this.OutputItemType = ItemType.Water;
            this.TryDistribute();
        }

        protected override void TryDistribute()
        {
            if (this.OutputItemCount > 0)
            {
                if (World.ResourceNetwork?.CanAddItem(ItemType.Water) == true)
                {
                    World.ResourceNetwork.AddItem(this.OutputItemType);
                    this.OutputItemCount--;
                    WorldStats.Increment(WorldStatKeys.WaterPumped);
                }
                else if (this.networkFullCooldown == 0) this.networkFullCooldown = 300;
            }

            if (this.OutputItemCount == 0)
            {
                this.OutputItemType = ItemType.None;
                this.FactoryProgress = 0.0;
                if (this.framesToBreak > 0 && this.MaintenanceLevel < 0.0001) this.FactoryStatus = FactoryStatus.Broken;
                else if (this.IsSwitchedOn) this.TryStart();
                else this.FactoryStatus = FactoryStatus.Offline;
            }
        }

        public override bool CanRecycle()
        {
            if (!base.CanRecycle())
            {
                this.canRecycleReasonStringId = StringsForMouseCursor.InUse;
                return false;
            }

            if (this.resourceContainer.StorageLevel > 0)
            {
                this.canRecycleReasonStringId = StringsForMouseCursor.NotEmpty;
                return false;
            }

            return true;
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{base.GetTextureName()}_{this.Direction.ToString()}";
        }

        protected override void UpdateAnimationFrame()
        {
            if (this.animationTimer == 0)
            {
                this.animationTimer = 3;
                var frame = this.AnimationFrame;

                switch (this.FactoryStatus)
                {
                    case FactoryStatus.InProgress:
                        frame = frame < 12 ? frame + 1 : 4;
                        break;
                    case FactoryStatus.Stopping:
                    case FactoryStatus.Pausing:
                    case FactoryStatus.Initialising:
                    case FactoryStatus.Starting:
                    case FactoryStatus.Resuming:
                    case FactoryStatus.Standby:
                    case FactoryStatus.Paused:
                        frame = (frame > 3 && frame < 12) ? frame + 1 : 3;
                        break;
                    case FactoryStatus.Broken:
                    case FactoryStatus.TooCold:
                    case FactoryStatus.TooDry:
                        frame = (frame > 3 && frame < 12) ? frame + 1 : 2;
                        break;
                    default:
                        frame = (frame > 3 && frame < 12) ? frame + 1 : 1;
                        break;
                }

                this.AnimationFrame = frame;
            }
            else this.animationTimer--;
        }
    }
}
