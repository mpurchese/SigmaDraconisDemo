namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class GlassFactory : FactoryBuilding, IEnergyConsumer, IResourceProviderBuilding, IResourceConsumerBuilding, IRepairableThing, IRotatableThing
    {
        private int animationStep;
        private int animationTimer;

        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        public GlassFactory() : base()
        {
        }

        public GlassFactory(ISmallTile mainTile, Direction direction) : base(ThingType.GlassFactory, mainTile, 1)
        {
            this.Direction = direction;
        }

        protected override void Init()
        {
            this.framesToInitialise = (int)(3600 * Constants.GlassFactoryEnergyStore / Constants.GlassFactoryEnergyUse);
            this.framesToProcess = Constants.GlassFactoryFramesToProcess;
            this.framesToPauseResume = Constants.GlassFactoryFramesToPauseResume;
            this.energyPerHour = Energy.FromKwH(Constants.GlassFactoryEnergyUse);
            this.energyPerFrame = energyPerHour / Constants.FramesPerHour;
            this.capacitorSize = Constants.GlassFactoryEnergyStore;
            this.consumedItemType = ItemType.Stone;
            this.producedItemType = ItemType.Glass;
            base.Init();
        }

        protected override void CompleteProcessing()
        {
            this.FactoryProgress = 1.0;
            this.pauseResumeFrameCounter = 0;
            this.FactoryStatus = FactoryStatus.WaitingToDistribute;
            this.InputItemType = ItemType.None;
            this.OutputItemType = ItemType.Glass;
            this.OutputItemCount = 2;
            this.TryDistribute();
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{base.GetTextureName()}_{this.Direction.ToString()}";
        }

        protected override void Process(double rate)
        {
            base.Process(rate);
            this.smokeSoundRate = Math.Max(0, this.animationFrame - 4) / 8f;
        }

        protected override void UpdateAnimationFrame()
        {
            if (this.animationTimer > 0)
            {
                this.animationTimer--;
                return;
            }

            this.animationTimer = 1;
            int frame;
            switch (this.FactoryStatus)
            {
                case FactoryStatus.InProgress:
                    if (this.animationStep > 24) frame = 36 - this.animationStep;
                    else if (this.animationStep > 18) frame = this.animationStep - 12;
                    else frame = 6;
                    this.animationStep++;
                    if (this.animationStep > 30) this.animationStep = 0;
                    this.smokeSoundRate = frame / 12f;
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
