namespace SigmaDraconis.World.Buildings
{
    using System;
    using System.Collections.Generic;
    using Projects;
    using ProtoBuf;
    using Rooms;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class AlgaePool : FactoryBuilding, IAlgaePool
    {
        [ProtoMember(2)]
        public double GrowthRate { get; private set; }

        [ProtoMember(4)]
        public bool IsTooHot { get; private set; }

        [ProtoMember(5)]
        public bool IsTooCold { get; private set; }

        [ProtoMember(6)]
        public bool IsTooDark { get; private set; }

        [ProtoMember(7)]
        public bool AutoFill { get; set; }

        [ProtoMember(8)]
        public int AnimationDelayTimer { get; set; }

        [ProtoMember(9)]
        public bool AutoHarvest { get; set; }

        [ProtoMember(10)]
        public bool IsWaterAvailable { get; set; }

        [ProtoMember(11)]
        public Dictionary<string, int> GrowthRateModifiers { get; private set; } = new Dictionary<string, int>();

        [ProtoMember(12)]
        public float Light { get; protected set; }

        [ProtoMember(13)]
        private bool isDoingManualHarvest;

        // For deserialization
        private AlgaePool() : base()
        {
        }

        public AlgaePool(ISmallTile tile) : base(ThingType.AlgaePool, tile, 2)
        {
        }

        protected override void Init()
        {
            this.producedItemType = ItemType.Biomass;
            base.Init();
        }

        public override void AfterAddedToWorld()
        {
            if (this.IsReady)
            {
                if (this.GrowthRateModifiers == null) this.GrowthRateModifiers = new Dictionary<string, int>();
                this.UpdateRoom();
            }

            base.AfterAddedToWorld();
        }

        public override void AfterRemoveFromWorld()
        {
            EventManager.EnqueueWorldPropertyChangeEvent(this.MainTileIndex, nameof(ISmallTile.IsMineResourceVisible), this.MainTile.Row);
            base.AfterRemoveFromWorld();
        }

        public override void AfterConstructionComplete()
        {
            this.AutoFill = true;
            this.AutoHarvest = true;
            if (this.CanFill()) this.Fill();
            else this.FactoryStatus = FactoryStatus.Standby;
            base.AfterConstructionComplete();
        }

        public override Energy UpdateFactory()
        {
            var energyUsed = (Energy)0;
            this.EnergyUseRate = 0;

            if (this.isDesignatedForRecycling) return energyUsed;

            if (this.OutputItemCount == 0) this.isDoingManualHarvest = false;
            if (this.FactoryStatus == FactoryStatus.WaitingToDistribute && (this.AutoHarvest || this.isDoingManualHarvest)) this.TryDistribute();

            this.Light = 0f;
            this.Temperature = 0f;
            foreach (var tile in this.allTiles)
            {
                this.Light += RoomManager.GetTileLightLevel(tile.Index) * 0.25f;
                this.Temperature += RoomManager.GetTileTemperature(tile.Index) * 0.25f;
            }

            var effectiveLight = WorldLight.GetEffectiveLight(this.Light);
            if (ProjectManager.GetDefinition(4)?.IsDone == true) effectiveLight = (effectiveLight + 1f) * 0.5f;

            this.IsTooHot = this.Temperature >= 45;
            this.IsTooCold = this.Temperature <= 0;
            this.IsTooDark = effectiveLight <= 0.0;
            this.GrowthRate = 0;
            this.GrowthRateModifiers.Clear();

            if (this.FactoryStatus == FactoryStatus.Offline || this.FactoryStatus == FactoryStatus.Standby || this.FactoryStatus == FactoryStatus.Initialising || this.FactoryStatus == FactoryStatus.NoResource) this.TryStart();

            if (this.FactoryStatus == FactoryStatus.InProgress && !this.IsTooCold && !this.IsTooHot && !this.IsTooDark)
            {
                var temperatureEffect = 0.0;
                if (this.Temperature >= 35 && this.Temperature < 45) temperatureEffect = (45 - this.Temperature) / 10.0;
                else if (this.Temperature >= 25 && this.Temperature < 35) temperatureEffect = 1.0;
                else if (this.Temperature < 25 && this.Temperature >= 0)
                {
                    temperatureEffect = this.Temperature / 25.0;
                    if (ProjectManager.GetDefinition(5)?.IsDone == true) temperatureEffect = (temperatureEffect + 1.0) / 2.0;
                }

                var projectEffect = ProjectManager.GetDefinition(2)?.IsDone == true ? Constants.AlgaeGrowthRateImproved / Constants.AlgaeGrowthRate : 1.0;

                this.GrowthRateModifiers.Add("Light", (int)(effectiveLight * 100));
                this.GrowthRateModifiers.Add("Temperature", (int)(temperatureEffect * 100));
                if (projectEffect > 1.0001) this.GrowthRateModifiers.Add("Projects", (int)(projectEffect * 100));

                this.GrowthRate = effectiveLight * temperatureEffect * projectEffect;
                if (this.GrowthRate > 0.0)
                {
                    this.FactoryProgress += this.GrowthRate * Constants.AlgaeGrowthRate;
                    if (this.FactoryProgress >= 1.0)
                    {
                        this.FactoryProgress = 1.0;
                        this.OutputItemType = ItemType.Biomass;
                        this.FactoryStatus = FactoryStatus.WaitingToDistribute;
                        this.OutputItemCount = ProjectManager.GetDefinition(3)?.IsDone == true ? Constants.AlgaeYieldImproved : Constants.AlgaeYield;
                    }
                }
            }

            this.UpdateAnimationFrame();

            return 0;
        }

        protected override void UpdateAnimationFrame()
        {
            if (this.AnimationDelayTimer == 0)
            {
                if (this.FactoryStatus == FactoryStatus.WaitingToDistribute)
                {
                    var f = 24 - (this.OutputItemCount * 2);
                    if (this.AnimationFrame < f) this.AnimationFrame++;
                }
                else if (this.FactoryStatus == FactoryStatus.InProgress)
                {
                    if (this.AnimationFrame == 24) this.AnimationFrame = 1;
                    else if (this.AnimationFrame > 8 + (int)(this.FactoryProgress * 8.0) || this.AnimationFrame < 8) this.AnimationFrame++;
                    else this.AnimationFrame = 8 + (int)(this.FactoryProgress * 8.0);
                }
                else if (this.AnimationFrame > 1)
                {
                    if (this.AnimationFrame == 24) this.AnimationFrame = 1;
                    else if (this.AnimationFrame >= 16) this.AnimationFrame++;
                    else this.AnimationFrame--;
                }

                this.AnimationDelayTimer = 3;
            }
            else
            {
                this.AnimationDelayTimer--;
            }

            base.UpdateAnimationFrame();
        }

        protected override void TryStart()
        {
            this.FactoryProgress = 0.0;

            if (this.AutoFill && this.CanFill() && !this.IsTooCold && !this.IsTooHot && !this.IsTooDark)
            {
                this.Fill();
            }
            else
            {
                this.FactoryStatus = this.IsWaterAvailable && !this.IsTooCold && !this.IsTooHot && !this.IsTooDark ? FactoryStatus.Standby : FactoryStatus.NoResource;
            }
        }

        public bool CanFill()
        {
            this.IsWaterAvailable = World.ResourceNetwork?.CanTakeItems(this, ItemType.Water, Constants.AlgaePoolWaterUse) == true;
            return this.IsWaterAvailable 
                && (this.FactoryStatus == FactoryStatus.Offline || this.FactoryStatus == FactoryStatus.Standby || this.FactoryStatus == FactoryStatus.Initialising || this.FactoryStatus == FactoryStatus.NoResource);
        }

        public bool CanDrain()
        {
            return this.FactoryStatus != FactoryStatus.Offline && this.FactoryStatus != FactoryStatus.Standby;
        }

        public bool CanHarvest()
        {
            if (this.FactoryStatus != FactoryStatus.WaitingToDistribute) return false;

            return World.ResourceNetwork?.CanAddItem(ItemType.Biomass) == true;
        }

        public override bool CanTakeOutput(ItemType itemType)
        {
            if (!isDoingManualHarvest && this.InventoryTarget.HasValue && World.ResourceNetwork?.GetItemTotal(ItemType.Biomass) >= this.InventoryTarget.Value) return false;

            return (this.AutoHarvest || this.isDoingManualHarvest) && base.CanTakeOutput(itemType);
        }

        public void Fill()
        {
            this.FactoryProgress = 0;
            this.FactoryStatus = FactoryStatus.InProgress;
            World.ResourceNetwork.TakeItems(this, ItemType.Water, Constants.AlgaePoolWaterUse);
        }

        public void Drain()
        {
            this.FactoryProgress = 0;
            this.AutoFill = false;
            this.FactoryStatus = FactoryStatus.Offline;
        }

        public void Harvest()
        {
            this.isDoingManualHarvest = true;
            this.TryDistribute();
        }

        protected override void TryDistribute()
        {
            if (!isDoingManualHarvest && this.InventoryTarget.HasValue && World.ResourceNetwork?.GetItemTotal(ItemType.Biomass) >= this.InventoryTarget.Value) return;

            var itemCount = this.OutputItemCount;
            base.TryDistribute();
            if (this.OutputItemCount < itemCount) WorldStats.Increment(WorldStatKeys.AlgaeHarvested, itemCount - this.OutputItemCount);
        }
    }
}
