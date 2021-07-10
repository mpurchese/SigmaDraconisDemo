namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class CoalPower : FactoryBuilding, IPowerPlant, IRotatableThing, IWaterConsumer
    {
        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        [ProtoMember(2)]
        public int BurnRateSetting { get; set; }

        [ProtoMember(3)]
        public double ConsumptionRate { get; private set; }

        public CoalPower() : base()
        {
        }

        public CoalPower(ISmallTile mainTile, Direction direction) : base(ThingType.CoalPower, mainTile, 2)
        {
            this.Direction = direction;
        }

        protected override void Init()
        {
            this.framesToPauseResume = Constants.CoalPowerFramesToPauseResume;
            this.framesToInitialise = Constants.CoalPowerFramesToInitialise;
            this.framesToProcess = Constants.CoalPowerFramesToProcess;
            this.energyPerHour = Energy.FromKwH(Constants.CoalPowerEnergyOutput);
            this.energyPerFrame = this.energyPerHour / Constants.FramesPerHour;
            this.consumedItemType = ItemType.Coal;
            base.Init();
        }

        public override void AfterConstructionComplete()
        {
            this.FactoryStatus = FactoryStatus.Offline;
            this.FactoryProgress = 0;
            this.IsSwitchedOn = true;
            this.MaintenanceLevel = 1.0;
            this.BurnRateSetting = 3;
            this.RepairPriority = WorkPriority.Normal;
            base.AfterConstructionComplete();
        }

        public override bool CanAddInput(ItemType itemType)
        {
            return World.ResourceNetwork?.IsEnergyFull == false 
                && itemType == ItemType.Coal
                && this.IsSwitchedOn && (this.FactoryStatus == FactoryStatus.Standby || this.FactoryProgress > 0.999)
                && this.InputItemType == ItemType.None;
        }

        protected override int GetWaterUseRate()
        {
            return (int)(this.EnergyGenRate.KWh * Constants.CoalPowerWaterUsePerKwH);
        }

        protected override void TryStart()
        {
            if (this.IsSwitchedOn && World.ResourceNetwork?.IsEnergyFull == false && this.MaintenanceLevel > 0.0)
            {
                if (World.ResourceNetwork.TakeItems(this, ItemType.Coal, 1) == 1)
                {
                    this.InputItemType = ItemType.Coal;
                }

                if (this.InputItemType != ItemType.None)
                {
                    this.FactoryProgress = 0.0;
                    this.FactoryStatus = this.pauseResumeFrameCounter < this.framesToPauseResume ? FactoryStatus.Starting : FactoryStatus.InProgress;
                }
                else if (this.FactoryStatus == FactoryStatus.Paused && this.FactoryProgress > 0.999)
                {
                    this.FactoryProgress = 0.0;
                    this.FactoryStatus = FactoryStatus.Standby;
                }
            }
        }

        public double BurnRateSettingToKW(int setting)
        {
            return Constants.CoalPowerEnergyOutput * (setting / 3.0);
        }

        protected override double GetGeneratorTargetProcessingRate()
        {
            return Math.Min(base.GetGeneratorTargetProcessingRate(), this.BurnRateSetting / 3.0);
        }

        protected override void Process(double rate)
        {
            this.smokeSoundRate = rate;
            this.DoSmoke(rate);

            var targetRate = this.GetGeneratorTargetProcessingRate();
            this.ConsumptionRate = (targetRate * 3600) / this.framesToProcess;
            this.FactoryProgress += targetRate / this.framesToProcess;

            if (this.FactoryProgress > 0.999 && this.InputItemType != ItemType.None)
            {
                WorldStats.Increment(WorldStatKeys.CoalBurned);
                this.InputItemType = ItemType.None;
            }
        }

        public override void Update()
        {
            if (this.IsDesignatedForRecycling)
            {
                this.IsSwitchedOn = false;
            }

            base.Update();
        }

        protected override void UpdateAnimationFrame()
        {
            if (this.FactoryStatus == FactoryStatus.InProgress || this.FactoryStatus == FactoryStatus.Resuming) this.AnimationFrame = 2;  // Green
            else if (this.FactoryStatus.In(FactoryStatus.Starting, FactoryStatus.Standby, FactoryStatus.Pausing, FactoryStatus.Paused, FactoryStatus.Stopping)) this.AnimationFrame = 3;   // Amber
            else if (this.FactoryStatus == FactoryStatus.Broken) this.AnimationFrame = World.WorldTime.Minute % 2 == 0 ? 1 : 4;   // Flashing red
            else this.AnimationFrame = 1;  // Off
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{base.GetTextureName()}_{this.Direction.ToString()}";
        }
    }
}

