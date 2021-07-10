namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class BiomassPower : FactoryBuilding, IBiomassPower, IRotatableThing
    {
        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        [ProtoMember(2)]
        public int BurnRateSetting { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public bool AllowBurnMush { get; set; }

        [ProtoMember(4, IsRequired = true)]
        public bool AllowBurnOrganics { get; set; }

        [ProtoMember(5)]
        public double ConsumptionRate { get; private set; }

        public BiomassPower() : base()
        {
        }

        public BiomassPower(ISmallTile mainTile, Direction direction) : base(ThingType.BiomassPower, mainTile, 2)
        {
            this.Direction = direction;
        }

        protected override void Init()
        {
            this.framesToPauseResume = Constants.BiomassPowerFramesToPauseResume;
            this.framesToInitialise = Constants.BiomassPowerFramesToInitialise;
            this.energyPerHour = Energy.FromKwH(Constants.BiomassPowerEnergyOutput);
            this.energyPerFrame = this.energyPerHour / Constants.FramesPerHour;
            base.Init();
        }

        public override void AfterConstructionComplete()
        {
            this.FactoryStatus = FactoryStatus.Offline;
            this.FactoryProgress = 0;
            this.IsSwitchedOn = true;
            this.MaintenanceLevel = 1.0;
            this.BurnRateSetting = 3;
            this.AllowBurnOrganics = true;
            this.RepairPriority = WorkPriority.Normal;
            base.AfterConstructionComplete();
        }

        public override bool CanAddInput(ItemType itemType)
        {
            return World.ResourceNetwork?.IsEnergyFull == false 
                && ((this.AllowBurnOrganics && itemType == ItemType.Biomass) || (this.AllowBurnMush && itemType == ItemType.Mush))
                && this.IsSwitchedOn && (this.FactoryStatus == FactoryStatus.Standby || this.FactoryProgress > 0.999)
                && this.InputItemType == ItemType.None;
        }

        protected override int GetWaterUseRate()
        {
            return (int)(this.EnergyGenRate.KWh * Constants.BiomassPowerWaterUsePerKwH);
        }

        protected override void TryStart()
        {
            if (this.IsSwitchedOn && World.ResourceNetwork?.IsEnergyFull == false && this.MaintenanceLevel > 0.0)
            {
                if (this.AllowBurnOrganics && World.ResourceNetwork.TakeItems(this, ItemType.Biomass, 1) == 1)
                {
                    this.InputItemType = ItemType.Biomass;
                }
                else if (this.AllowBurnMush && World.ResourceNetwork.TakeItems(this, ItemType.Mush, 1) == 1)
                {
                    this.InputItemType = ItemType.Mush;
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

        protected override void Process(double rate)
        {
            this.smokeSoundRate = rate;
            this.DoSmoke(rate);

            var frames = this.InputItemType == ItemType.Mush ? Constants.BiomassPowerFramesToProcessMush : Constants.BiomassPowerFramesToProcessOrganics;
            var targetRate = this.GetGeneratorTargetProcessingRate();
            this.ConsumptionRate = (targetRate * 3600) / frames;
            this.FactoryProgress += targetRate / frames;
            if (this.FactoryProgress > 0.999 && this.InputItemType != ItemType.None)
            {
                WorldStats.Increment(WorldStatKeys.OrganicsBurned);
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

        public double BurnRateSettingToKW(int setting)
        {
            return Constants.BiomassPowerEnergyOutput * (setting / 3.0);
        }

        protected override double GetGeneratorTargetProcessingRate()
        {
            return Math.Min(base.GetGeneratorTargetProcessingRate(), this.BurnRateSetting / 3.0);
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

