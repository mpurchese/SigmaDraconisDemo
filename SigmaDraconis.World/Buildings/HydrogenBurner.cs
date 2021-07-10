namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class HydrogenBurner : FactoryBuilding, IPowerPlant, IWaterConsumer
    {
        [ProtoMember(1)]
        public int BurnRateSetting { get; set; }

        [ProtoMember(2)]
        public double ConsumptionRate { get; private set; }

        public HydrogenBurner() : base()
        {
        }

        public HydrogenBurner(ISmallTile mainTile) : base(ThingType.HydrogenBurner, mainTile, 1)
        {
        }

        protected override void Init()
        {
            this.framesToPauseResume = Constants.HydrogenBurnerFramesToPauseResume;
            this.framesToInitialise = Constants.HydrogenBurnerFramesToInitialise;
            this.framesToProcess = Constants.HydrogenBurnerFramesToProcess;
            this.energyPerHour = Energy.FromKwH(Constants.HydrogenBurnerEnergyOutput);
            this.energyPerFrame = this.energyPerHour / Constants.FramesPerHour;
            this.consumedItemType = ItemType.LiquidFuel;
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
                && itemType == ItemType.LiquidFuel
                && this.IsSwitchedOn && (this.FactoryStatus == FactoryStatus.Standby || this.FactoryProgress > 0.999)
                && this.InputItemType == ItemType.None;
        }

        protected override void TryStart()
        {
            if (this.IsSwitchedOn && World.ResourceNetwork?.IsEnergyFull == false && this.MaintenanceLevel > 0.0)
            {
                if (World.ResourceNetwork.TakeItems(this, ItemType.LiquidFuel, 1) == 1)
                {
                    this.InputItemType = ItemType.LiquidFuel;
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
            switch(setting)
            {
                case 1: return 15.0;
                case 2: return 30.0;
            }

            return Constants.HydrogenBurnerEnergyOutput;
        }

        protected override int GetWaterUseRate()
        {
            return (int)(this.EnergyGenRate.KWh * Constants.HydrogenBurnerWaterUsePerKwH);
        }

        protected override double GetGeneratorTargetProcessingRate()
        {
            var setting = Math.Max(this.BurnRateSetting, 1);
            return Math.Min(base.GetGeneratorTargetProcessingRate(), this.BurnRateSettingToKW(setting) / Constants.HydrogenBurnerEnergyOutput);
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
                WorldStats.Increment(WorldStatKeys.HydrogenBurned);
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
            switch (this.FactoryStatus)
            {
                case FactoryStatus.InProgress: 
                    this.AnimationFrame = 20; 
                    break;  // Green
                case FactoryStatus.Resuming:
                case FactoryStatus.Starting:
                case FactoryStatus.Pausing:
                case FactoryStatus.Stopping:
                    var r = this.pauseResumeFrameCounter / (double)this.framesToPauseResume;
                    var f = (4 + (int)(r * 16f)).Clamp(4, 20);
                    this.AnimationFrame = f > 4 ? f : 2;   // Green, flame gets brighter / dimmer
                    break;
                case FactoryStatus.Standby:
                case FactoryStatus.Paused:
                    this.AnimationFrame = 3;   // Amber
                    break;
                case FactoryStatus.Broken:
                    this.AnimationFrame = World.WorldTime.Minute % 2 == 0 ? 1 : 4;   // Flashing red
                    break;
                default:
                    this.AnimationFrame = 1;  // Off
                    break;
            }
        }
    }
}

