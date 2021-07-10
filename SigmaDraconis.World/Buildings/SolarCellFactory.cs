namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class SolarCellFactory : FactoryBuilding, IEnergyConsumer, IResourceProviderBuilding, IResourceConsumerBuilding, IRepairableThing, IRotatableThing
    {
        private int animationStep;
        private int animationTimer;

        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        [ProtoMember(2)]
        public bool HasStone { get; private set; }

        [ProtoMember(3)]
        public bool HasMetal { get; private set; }

        public SolarCellFactory() : base()
        {
        }

        public SolarCellFactory(ISmallTile mainTile, Direction direction) : base(ThingType.SolarCellFactory, mainTile, 2)
        {
            this.Direction = direction;
        }

        protected override void Init()
        {
            this.framesToInitialise = (int)(3600 * Constants.SolarCellFactoryEnergyStore / Constants.SolarCellFactoryEnergyUse);
            this.framesToProcess = Constants.SolarCellFactoryFramesToProcess;
            this.framesToPauseResume = Constants.SolarCellFactoryFramesToPauseResume;
            this.energyPerHour = Energy.FromKwH(Constants.SolarCellFactoryEnergyUse);
            this.energyPerFrame = energyPerHour / Constants.FramesPerHour;
            this.capacitorSize = Constants.SolarCellFactoryEnergyStore;
            this.producedItemType = ItemType.SolarCells;
            base.Init();
        }

        protected override void CompleteProcessing()
        {
            this.FactoryProgress = 1.0;
            this.pauseResumeFrameCounter = 0;
            this.FactoryStatus = FactoryStatus.WaitingToDistribute;
            this.InputItemType = ItemType.None;
            this.OutputItemType = ItemType.SolarCells;
            this.OutputItemCount = 4;
            this.TryDistribute();
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{base.GetTextureName()}_{this.Direction.ToString()}";
        }

        public override bool CanAddInput(ItemType itemType)
        {
            if (World.ResourceNetwork == null) return false;
            if (this.FactoryStatus != FactoryStatus.Standby || !this.IsSwitchedOn) return false;
            if (this.InventoryTarget.HasValue && World.ResourceNetwork.GetItemTotal(this.producedItemType) >= this.InventoryTarget.Value) return false;
            if (itemType == ItemType.Stone && !this.HasStone && (this.HasMetal || World.ResourceNetwork.CanTakeItems(this, ItemType.Metal, 1))) return true;
            if (itemType == ItemType.Metal && !this.HasMetal && (this.HasStone || World.ResourceNetwork.CanTakeItems(this, ItemType.Stone, 1))) return true;

            return false;
        }

        public override void AddInput(ItemType itemType)
        {
            if (World.ResourceNetwork == null) return;

            if (itemType == ItemType.Metal && !this.HasStone) World.ResourceNetwork.TakeItems(this, ItemType.Stone, 1);
            else if (itemType == ItemType.Stone && !this.HasMetal) World.ResourceNetwork.TakeItems(this, ItemType.Metal, 1);

            this.HasMetal = true;
            this.HasStone = true;
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
            var stoneAvailable = this.HasStone || network.CanTakeItems(this, ItemType.Stone, 1);

            if (metalAvailable && stoneAvailable)
            {
                if (this.CapacitorCharge >= this.capacitorSize || network.CanTakeEnergy(Energy.FromKwH(this.capacitorSize - this.CapacitorCharge)))
                {
                    if (!this.HasMetal) network.TakeItems(this, ItemType.Metal, 1);
                    if (!this.HasStone) network.TakeItems(this, ItemType.Stone, 1);
                    this.FactoryStatus = FactoryStatus.InProgress;
                    this.HasMetal = false;
                    this.HasStone = false;
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
            var frame = this.animationFrame;

            switch (this.FactoryStatus)
            {
                case FactoryStatus.InProgress:
                    if (frame < 8) frame++;
                    else if (this.animationStep > 12) frame = 24 - this.animationStep;
                    else frame = Math.Max(this.animationStep, 8); 
                    this.animationStep++;
                    if (this.animationStep > 16) this.animationStep = 0;
                    this.smokeSoundRate = (this.animationFrame - 4) / 16f;
                    break;
                case FactoryStatus.Stopping:
                case FactoryStatus.Pausing:
                case FactoryStatus.Initialising:
                case FactoryStatus.Starting:
                case FactoryStatus.Resuming:
                case FactoryStatus.Standby:
                case FactoryStatus.Paused:
                    frame = 3;
                    this.animationStep = 0;
                    this.smokeSoundRate = 0;
                    break;
                case FactoryStatus.Broken:
                case FactoryStatus.WaitingToDistribute:
                    frame = 2;
                    this.animationStep = 0;
                    this.smokeSoundRate = 0;
                    break;
                default:
                    frame = 1;
                    this.animationStep = 0;
                    this.smokeSoundRate = 0;
                    break;
            }

            this.AnimationFrame = frame.Clamp(this.animationFrame - 1, this.animationFrame + 1);
        }

        protected override void BeforeRecycle()
        {
            this.AnimationFrame = 1;
            base.BeforeRecycle();
        }
    }
}
