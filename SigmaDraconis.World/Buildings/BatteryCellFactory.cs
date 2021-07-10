namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class BatteryCellFactory : FactoryBuilding, IEnergyConsumer, IResourceProviderBuilding, IResourceConsumerBuilding, IRepairableThing, IRotatableThing
    {
        private int animationStep;
        private int animationTimer;

        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        public BatteryCellFactory() : base()
        {
        }

        public BatteryCellFactory(ISmallTile mainTile, Direction direction) : base(ThingType.BatteryCellFactory, mainTile, 1)
        {
            this.Direction = direction;
        }

        protected override void Init()
        {
            this.framesToInitialise = (int)(3600 * Constants.BatteryCellFactoryEnergyStore / Constants.BatteryCellFactoryEnergyUse);
            this.framesToProcess = Constants.BatteryCellFactoryFramesToProcess;
            this.framesToPauseResume = Constants.BatteryCellFactoryFramesToPauseResume;
            this.energyPerHour = Energy.FromKwH(Constants.BatteryCellFactoryEnergyUse);
            this.energyPerFrame = energyPerHour / Constants.FramesPerHour;
            this.capacitorSize = Constants.BatteryCellFactoryEnergyStore;
            this.consumedItemType = ItemType.Metal;
            this.producedItemType = ItemType.BatteryCells;
            base.Init();
        }

        protected override void CompleteProcessing()
        {
            this.FactoryProgress = 1.0;
            this.pauseResumeFrameCounter = 0;
            this.FactoryStatus = FactoryStatus.WaitingToDistribute;
            this.InputItemType = ItemType.None;
            this.OutputItemType = ItemType.BatteryCells;
            this.OutputItemCount = 2;
            this.TryDistribute();
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{base.GetTextureName()}_{this.Direction}";
        }

        protected override void Process(double rate)
        {
            base.Process(rate);
            this.smokeSoundRate = this.animationFrame / 12f;
        }

        protected override void UpdateAnimationFrame()
        {
            if (this.animationTimer > 0)
            {
                this.animationTimer--;
                return;
            }

            this.animationTimer = 1;

            switch (this.FactoryStatus)
            {
                case FactoryStatus.InProgress:
                    if (this.animationStep > 24) this.AnimationFrame = 36 - this.animationStep;
                    else if (this.animationStep > 16) this.AnimationFrame = this.animationStep - 12;
                    else this.animationFrame = 4;
                    this.animationStep++;
                    if (this.animationStep > 32) this.animationStep = 0;
                    this.smokeSoundRate = this.animationFrame / 12f;
                    break;
                case FactoryStatus.Stopping:
                case FactoryStatus.Pausing:
                case FactoryStatus.Initialising:
                case FactoryStatus.Starting:
                case FactoryStatus.Resuming:
                case FactoryStatus.Standby:
                case FactoryStatus.Paused:
                    this.AnimationFrame = 3;
                    this.animationStep = 0;
                    this.smokeSoundRate = 0;
                    break;
                case FactoryStatus.Broken:
                case FactoryStatus.WaitingToDistribute:
                    this.AnimationFrame = 2;
                    this.animationStep = 0;
                    this.smokeSoundRate = 0;
                    break;
                default:
                    this.AnimationFrame = 1;
                    this.animationStep = 0;
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
