namespace SigmaDraconis.World.Buildings
{
    using System;
    using ProtoBuf;
    using Language;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class WaterPump : FactoryBuilding, IEnergyConsumer, IWaterPump, IRotatableThing, IRepairableThing, ISilo
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

        // For GroundWaterController
        [ProtoMember(5)]
        public int? RequestingWaterFromGroundTile { get; set; }

        // Populated by GroundWaterController
        [ProtoMember(6)]
        public int ExtractionRate { get; set; }

        [ProtoMember(7)]
        public int ExtractionCounter { get; private set; }

        [ProtoMember(8)]
        public int WaterGenRate { get; private set; }

        [ProtoMember(9)]
        private int networkFullCooldown;

        public int StorageLevel => this.resourceContainer.StorageLevel;
        public int StorageCapacity => this.resourceContainer.StorageCapacity;

        public WaterPump() : base()
        {
        }

        public WaterPump(ISmallTile mainTile, Direction direction) : base(ThingType.WaterPump, mainTile, 1)
        {
            this.Direction = direction;
            this.resourceContainer = new ResourceContainer(Constants.WaterPumpCapacity);
        }

        // To be called by GroundWaterController
        public void AddWaterFromGround(int amount)
        {
            this.RequestingWaterFromGroundTile = null;
            this.OutputItemCount = amount;
            this.OutputItemType = amount > 0 ? ItemType.Water : ItemType.None;
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
            this.framesToInitialise = (int)(3600 * Constants.WaterPumpEnergyStore / Constants.WaterPumpEnergyUse);
            this.framesToPauseResume = Constants.WaterPumpFramesToPauseResume;
            this.framesToProcess = Constants.WaterPumpFramesToProcess;
            this.energyPerHour = Energy.FromKwH(Constants.WaterPumpEnergyUse);
            this.energyPerFrame = energyPerHour / Constants.FramesPerHour;
            this.capacitorSize = Constants.WaterPumpEnergyStore;
            this.producedItemType = ItemType.Water;
            this.minTemperature = Constants.WaterPumpMinTemperature;
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

            var targetWaterGenRate = this.FactoryStatus == FactoryStatus.InProgress ? (this.ExtractionRate * 2) : 0;
            if (targetWaterGenRate > this.WaterGenRate) this.WaterGenRate = Math.Min(targetWaterGenRate, this.WaterGenRate + 10);
            else if (targetWaterGenRate < this.WaterGenRate) this.WaterGenRate = Math.Max(targetWaterGenRate, this.WaterGenRate - 10);
        }

        protected override void TryStart()
        {
            // Only stop if pump is in standby position
            if ((this.animationFrame <= 3 && this.OutputItemCount > 0) || this.networkFullCooldown > 0)
            {
                this.FactoryStatus = FactoryStatus.WaitingToDistribute;
                return;
            }

            if (!this.CheckMoistureOK()) this.FactoryStatus = FactoryStatus.TooDry;
            else if (this.CapacitorCharge >= this.capacitorSize || World.ResourceNetwork?.CanTakeEnergy(Energy.FromKwH(this.capacitorSize - this.CapacitorCharge)) == true)
            {
                if (this.InputItemType != this.consumedItemType) World.ResourceNetwork.TakeItems(this, this.consumedItemType, 1);
                this.FactoryStatus = FactoryStatus.InProgress;
                this.InputItemType = ItemType.None;
            }
            else this.FactoryStatus = FactoryStatus.NoPower;

            if (this.FactoryStatus == FactoryStatus.InProgress)
            {
                // Request water from GroundWaterController
                var direction = (Direction)Math.Min(8, Rand.Next(10));
                var tile = direction == Direction.None ? this.mainTile : this.mainTile.GetTileToDirection(direction);
                this.RequestingWaterFromGroundTile = tile?.Index;
            }
        }

        protected override void Process(double rate)
        {
            // May have excess output if network was full
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
            
            this.ExtractionCounter += this.ExtractionRate;
            while (this.ExtractionCounter > 100)
            {
                this.ExtractionCounter -= 100;
                this.OutputItemCount++;
                this.OutputItemType = ItemType.Water;
            }
            
            this.TryDistribute();
        }

        protected override void TryDistribute()
        {
            while (this.OutputItemCount > 0 && World.ResourceNetwork?.CanAddItem(ItemType.Water) == true)
            {
                World.ResourceNetwork.AddItem(this.OutputItemType);
                this.OutputItemCount--;
                WorldStats.Increment(WorldStatKeys.WaterPumped);
            }

            if (this.OutputItemCount == 0)
            {
                this.OutputItemType = ItemType.None;
                this.FactoryProgress = 0.0;
                if (this.framesToBreak > 0 && this.MaintenanceLevel < 0.0001) this.FactoryStatus = FactoryStatus.Broken;
                else if (this.IsSwitchedOn) this.TryStart();
                else this.FactoryStatus = FactoryStatus.Offline;
            }
            else if (this.networkFullCooldown == 0) this.networkFullCooldown = 300;
        }

        protected override bool CheckMoistureOK()
        {
            return this.ExtractionRate > 0;
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
            var renderDirection = this.Direction == Direction.SE || this.Direction == Direction.NW ? "SE" : "SW";
            return $"{base.GetTextureName()}_{renderDirection}";
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
                        frame = frame < 48 ? frame + 1 : 3;
                        break;
                    case FactoryStatus.Stopping:
                    case FactoryStatus.Pausing:
                    case FactoryStatus.Initialising:
                    case FactoryStatus.Starting:
                    case FactoryStatus.Resuming:
                    case FactoryStatus.Standby:
                    case FactoryStatus.Paused:
                        frame = (frame > 3 && frame < 48) ? frame + 1 : 3;
                        break;
                    case FactoryStatus.Broken:
                    case FactoryStatus.TooCold:
                    case FactoryStatus.TooDry:
                        frame = (frame > 3 && frame < 48) ? frame + 1 : 2;
                        break;
                    default:
                        frame = (frame > 3 && frame < 48) ? frame + 1 : 1;
                        break;
                }

                this.AnimationFrame = frame;
            }
            else this.animationTimer--;

            this.smokeSoundRate = this.animationFrame > 36 ? (this.animationFrame - 36) / 12f : 0f;
        }
    }
}
