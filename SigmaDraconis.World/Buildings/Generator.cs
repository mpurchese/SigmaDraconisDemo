namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class Generator : FactoryBuilding, IGenerator, IRotatableThing, IWaterConsumer
    {
        [ProtoMember(10)]
        public Direction Direction { get; private set; }

        [ProtoMember(11, IsRequired = true)]
        public bool AllowBurnCoal { get; set; }

        [ProtoMember(12, IsRequired = true)]
        public bool AllowBurnOrganics { get; set; }

        [ProtoMember(13)]
        public double ConsumptionRate { get; private set; }

        public int BurnRateSetting { get; set; }

        public Generator() : base()
        {
        }

        public Generator(ISmallTile mainTile, Direction direction) : base(ThingType.Generator, mainTile, 1)
        {
            this.Direction = direction;
        }

        public double BurnRateSettingToKW(int setting)
        {
            return Constants.GeneratorEnergyOutputCoal;
        }

        protected override void Init()
        {
            this.framesToPauseResume = Constants.GeneratorFramesToPauseResume;
            this.framesToInitialise = Constants.GeneratorFramesToInitialise;
            base.Init();
        }

        public override void AfterConstructionComplete()
        {
            this.FactoryStatus = FactoryStatus.Offline;
            this.FactoryProgress = 0;
            this.IsSwitchedOn = true;
            this.AllowBurnCoal = true;
            this.AllowBurnOrganics = true;
            this.MaintenanceLevel = 1.0;
            this.BurnRateSetting = 3;
            this.RepairPriority = WorkPriority.Normal;
            base.AfterConstructionComplete();
        }

        public override void AfterDeserialization()
        {
            this.BurnRateSetting = 3;
            this.UpdateRates();
            base.AfterDeserialization();
        }

        public override bool CanAddInput(ItemType itemType)
        {
            return World.ResourceNetwork?.IsEnergyFull == false 
                && ((this.AllowBurnCoal && itemType == ItemType.Coal) || (this.AllowBurnOrganics && itemType == ItemType.Biomass))
                && this.IsSwitchedOn && (this.FactoryStatus == FactoryStatus.Standby || this.FactoryProgress > 0.999)
                && this.InputItemType == ItemType.None;
        }

        protected override void TryStart()
        {
            var network = World.ResourceNetwork;
            if (network == null) return;

            if (this.IsSwitchedOn && !network.IsEnergyFull && this.MaintenanceLevel > 0.0)
            {
                if (this.InputItemType == ItemType.Coal || (this.AllowBurnCoal && network.TakeItems(this, ItemType.Coal, 1) == 1))
                {
                    this.InputItemType = ItemType.Coal;
                }
                else if (this.InputItemType == ItemType.Biomass || (this.AllowBurnOrganics && network.TakeItems(this, ItemType.Biomass, 1) == 1))
                {
                    this.InputItemType = ItemType.Biomass;
                }

                if (this.InputItemType != ItemType.None)
                {
                    this.UpdateRates();
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

        private void UpdateRates()
        {
            switch (this.InputItemType)
            {
                case ItemType.Coal:
                    this.framesToProcess = Constants.GeneratorFramesToProcessCoal;
                    this.energyPerHour = Energy.FromKwH(Constants.GeneratorEnergyOutputCoal);
                    break;
                case ItemType.LiquidFuel:
                    this.framesToProcess = Constants.GeneratorFramesToProcessLiquidFuel;
                    this.energyPerHour = Energy.FromKwH(Constants.GeneratorEnergyOutputLiquidFuel);
                    break;
                default:   // Organics
                    this.framesToProcess = Constants.GeneratorFramesToProcessOrganics;
                    this.energyPerHour = Energy.FromKwH(Constants.GeneratorEnergyOutputOrganics);
                    break;
            }

            this.energyPerFrame = energyPerHour / Constants.FramesPerHour;
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
                switch (this.InputItemType)
                {
                    case ItemType.Coal: WorldStats.Increment(WorldStatKeys.CoalBurned); break;
                    case ItemType.Biomass: WorldStats.Increment(WorldStatKeys.OrganicsBurned); break;
                    case ItemType.LiquidFuel: WorldStats.Increment(WorldStatKeys.HydrogenBurned); break;
                }

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
            if (this.FactoryStatus == FactoryStatus.InProgress || this.FactoryStatus == FactoryStatus.Resuming)
            {
                switch (this.InputItemType)
                {
                    case ItemType.Biomass: this.AnimationFrame = 2; break;
                    case ItemType.Coal: this.AnimationFrame = 4; break;
                    case ItemType.LiquidFuel: this.AnimationFrame = 3; break;
                }
            }
            else if (this.FactoryStatus.In(FactoryStatus.Starting, FactoryStatus.Standby, FactoryStatus.Pausing, FactoryStatus.Paused, FactoryStatus.Stopping)) this.AnimationFrame = 5;   // Amber
            else if (this.FactoryStatus == FactoryStatus.Broken) this.AnimationFrame = World.WorldTime.Minute % 2 == 0 ? 1 : 6;   // Flashing red
            else this.AnimationFrame = 1;  // Off
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{base.GetTextureName()}_{this.Direction.ToString()}";
        }

        public override void UpdateShadowModel()
        {
            var shadowModel = new List<Vector3>();
            var x = this.MainTile.CentrePosition.X;
            var y = this.MainTile.CentrePosition.Y;

            if (this.Direction == Direction.E) this.Direction = Direction.SE;

            if (this.Direction == Direction.SE)
            {
                // Pipe 1
                this.AddVerticalShadowQuad(shadowModel, x, y + 0.2f, 0.4f, 3.5f, 1.6f, 3.5f, 0, 2);
                this.AddVerticalShadowQuad(shadowModel, x, y + 0.2f, 1f, 3.2f, 1f, 3.8f, 0, 2);
                this.AddVerticalShadowQuad(shadowModel, x, y + 0.2f, 0.4f, 3.5f, 1.6f, 3.5f, 2, 3, -1, -0.5f);
                this.AddVerticalShadowQuad(shadowModel, x, y + 0.2f, 1f, 3.2f, 1f, 3.8f, 2, 3, -1, -0.5f);

                // Pipe 2
                this.AddVerticalShadowQuad(shadowModel, x + 5.8f, y - 2.4f, 0.4f, 3.5f, 1.6f, 3.5f, 0, 2);
                this.AddVerticalShadowQuad(shadowModel, x + 5.8f, y - 2.4f, 1f, 3.2f, 1f, 3.8f, 0, 2);
                this.AddVerticalShadowQuad(shadowModel, x + 5.8f, y - 2.4f, 0.4f, 3.5f, 1.6f, 3.5f, 2, 3, -1, -0.5f);
                this.AddVerticalShadowQuad(shadowModel, x + 5.8f, y - 2.4f, 1f, 3.2f, 1f, 3.8f, 2, 3, -1, -0.5f);

                // Main cylinder
                this.AddHorizontalCylinderShadowQuad(shadowModel, x - 1.0f, y + 1.25f, -2.5f, 1.5f, 6.3f, -2.9f, 3.5f, 3.5f, 16);

                // Generator box
                this.AddVerticalShadowQuad(shadowModel, x, y, -8.5f, -0.5f, -6.5f, 0.5f, 0, 4f);
                this.AddVerticalShadowQuad(shadowModel, x, y, -8.5f, -0.5f, -6.5f, -1.5f, 0, 4f);
                this.AddVerticalShadowQuad(shadowModel, x, y, -4.5f, -0.5f, -6.5f, -1.5f, 0, 4f);
                this.AddVerticalShadowQuad(shadowModel, x, y, -4.5f, -0.5f, -6.5f, -1.5f, 0, 4f);

                this.AddVerticalShadowQuad(shadowModel, x, y, -6.5f, -1.5f, -3.5f, -3.0f, 0, 3.5f);

                this.AddVerticalShadowQuad(shadowModel, x, y, -3.5f, -3.0f, -2.5f, -3.5f, 0, 4f);
                this.AddVerticalShadowQuad(shadowModel, x, y, -3.5f, -3.0f, -2.5f, -2.5f, 0, 4f);

                // Chimney
                this.AddVerticalCylinderShadowQuad(shadowModel, x + 4.3f, y - 1.25f, 0.8f, 0.4f, 7f, 9.7f, 4, false);
            }
            else if (this.Direction == Direction.SW)
            {
                // Pipe 1
                this.AddVerticalShadowQuad(shadowModel, x, y + 0.2f, -0.4f, 3.5f, -1.6f, 3.5f, 0, 2);
                this.AddVerticalShadowQuad(shadowModel, x, y + 0.2f, -1f, 3.2f, -1f, 3.8f, 0, 2);
                this.AddVerticalShadowQuad(shadowModel, x, y + 0.2f, -0.4f, 3.5f, -1.6f, 3.5f, 2, 3, -1, -0.5f);
                this.AddVerticalShadowQuad(shadowModel, x, y + 0.2f, -1f, 3.2f, -1f, 3.8f, 2, 3, -1, -0.5f);

                // Pipe 2
                this.AddVerticalShadowQuad(shadowModel, x - 5.8f, y - 2.4f, -0.4f, 3.5f, -1.6f, 3.5f, 0, 2);
                this.AddVerticalShadowQuad(shadowModel, x - 5.8f, y - 2.4f, -1f, 3.2f, -1f, 3.8f, 0, 2);
                this.AddVerticalShadowQuad(shadowModel, x - 5.8f, y - 2.4f, -0.4f, 3.5f, -1.6f, 3.5f, 2, 3, -1, -0.5f);
                this.AddVerticalShadowQuad(shadowModel, x - 5.8f, y - 2.4f, -1f, 3.2f, -1f, 3.8f, 2, 3, -1, -0.5f);

                // Main cylinder
                this.AddShadowQuad(shadowModel, x, y, -4.90f, -1.38f, 0.03f, 2.89f, 2.51f, 0.03f, 1.95f, 2.98f, 0.32f, -5.84f, -0.91f, 0.32f);
                this.AddShadowQuad(shadowModel, x, y, -5.84f, -0.91f, 0.32f, 1.95f, 2.98f, 0.32f, 1.17f, 3.37f, 1.07f, -6.62f, -0.52f, 1.07f);
                this.AddShadowQuad(shadowModel, x, y, -6.62f, -0.52f, 1.07f, 1.17f, 3.37f, 1.07f, 0.62f, 3.65f, 2.20f, -7.18f, -0.25f, 2.20f);
                this.AddShadowQuad(shadowModel, x, y, -7.18f, -0.25f, 2.20f, 0.62f, 3.65f, 2.20f, 0.43f, 3.74f, 3.54f, -7.36f, -0.15f, 3.54f);
                this.AddShadowQuad(shadowModel, x, y, -7.36f, -0.15f, 3.54f, 0.43f, 3.74f, 3.54f, 0.62f, 3.65f, 4.84f, -7.18f, -0.25f, 4.84f);
                this.AddShadowQuad(shadowModel, x, y, -7.18f, -0.25f, 4.84f, 0.62f, 3.65f, 4.84f, 1.17f, 3.37f, 5.97f, -6.62f, -0.52f, 5.97f);
                this.AddShadowQuad(shadowModel, x, y, -6.62f, -0.52f, 5.97f, 1.17f, 3.37f, 5.97f, 1.95f, 2.98f, 6.73f, -5.84f, -0.91f, 6.73f);
                this.AddShadowQuad(shadowModel, x, y, -5.84f, -0.91f, 6.73f, 1.95f, 2.98f, 6.73f, 2.89f, 2.51f, 7.02f, -4.90f, -1.38f, 7.02f);
                this.AddShadowQuad(shadowModel, x, y, -4.90f, -1.38f, 7.02f, 2.89f, 2.51f, 7.02f, 3.83f, 2.04f, 6.73f, -3.95f, -1.86f, 6.73f);
                this.AddShadowQuad(shadowModel, x, y, -3.95f, -1.86f, 6.73f, 3.83f, 2.04f, 6.73f, 4.63f, 1.64f, 5.97f, -3.16f, -2.26f, 5.97f);
                this.AddShadowQuad(shadowModel, x, y, -3.16f, -2.26f, 5.97f, 4.63f, 1.64f, 5.97f, 5.17f, 1.37f, 4.84f, -2.62f, -2.52f, 4.84f);
                this.AddShadowQuad(shadowModel, x, y, -2.62f, -2.52f, 4.84f, 5.17f, 1.37f, 4.84f, 5.35f, 1.28f, 3.54f, -2.44f, -2.61f, 3.54f);
                this.AddShadowQuad(shadowModel, x, y, -2.44f, -2.61f, 3.54f, 5.35f, 1.28f, 3.54f, 5.17f, 1.37f, 2.20f, -2.62f, -2.52f, 2.20f);
                this.AddShadowQuad(shadowModel, x, y, -2.62f, -2.52f, 2.20f, 5.17f, 1.37f, 2.20f, 4.63f, 1.64f, 1.07f, -3.16f, -2.26f, 1.07f);
                this.AddShadowQuad(shadowModel, x, y, -3.16f, -2.26f, 1.07f, 4.63f, 1.64f, 1.07f, 3.83f, 2.04f, 0.32f, -3.96f, -1.86f, 0.32f);
                this.AddShadowQuad(shadowModel, x, y, -3.96f, -1.86f, 0.32f, 3.83f, 2.04f, 0.32f, 2.89f, 2.51f, 0.03f, -4.90f, -1.38f, 0.03f);

                // Generator
                this.AddVerticalShadowQuad(shadowModel, x, y, -1.50f, -2.76f, 0.64f, -3.82f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, 0.64f, -3.82f, 2.79f, -2.75f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, 2.79f, -2.75f, 0.66f, -1.68f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, 0.66f, -1.68f, -1.50f, -2.76f, 0, 4);
                this.AddShadowQuad(shadowModel, x, y, 1.99f, -2.22f, 0.73f, 6.09f, -0.17f, 0.73f, 6.68f, -0.47f, 1.07f, 2.58f, -2.52f, 1.07f);
                this.AddShadowQuad(shadowModel, x, y, 2.58f, -2.52f, 1.07f, 6.68f, -0.47f, 1.07f, 7.09f, -0.68f, 2.12f, 2.99f, -2.73f, 2.12f);
                this.AddShadowQuad(shadowModel, x, y, 2.99f, -2.73f, 2.12f, 7.09f, -0.68f, 2.12f, 6.77f, -0.51f, 3.25f, 2.67f, -2.56f, 3.25f);
                this.AddShadowQuad(shadowModel, x, y, 2.67f, -2.56f, 3.25f, 6.77f, -0.51f, 3.25f, 6.07f, -0.16f, 3.63f, 1.97f, -2.21f, 3.63f);
                this.AddVerticalShadowQuad(shadowModel, x, y, 4.94f, 0.38f, 7.14f, -0.72f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, 7.14f, -0.72f, 8.26f, -0.15f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, 8.26f, -0.15f, 6.07f, 0.94f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, 6.07f, 0.94f, 4.94f, 0.38f, 0, 4);

                // Chimney
                this.AddVerticalCylinderShadowQuad(shadowModel, x + 2.12f, y + 2.18f, 0.8f, 0.4f, 7f, 9.7f, 4, false);
            }
            else if (this.Direction == Direction.NW)
            {
                // Main cylinder
                this.AddShadowQuad(shadowModel, x, y, -5.02f, 1.45f, 0.03f, 2.77f, -2.45f, 0.03f, 3.71f, -1.98f, 0.32f, -4.08f, 1.92f, 0.32f);
                this.AddShadowQuad(shadowModel, x, y, -4.08f, 1.92f, 0.32f, 3.71f, -1.98f, 0.32f, 4.51f, -1.58f, 1.07f, -3.28f, 2.32f, 1.07f);
                this.AddShadowQuad(shadowModel, x, y, -3.28f, 2.32f, 1.07f, 4.51f, -1.58f, 1.07f, 5.04f, -1.31f, 2.20f, -2.75f, 2.58f, 2.20f);
                this.AddShadowQuad(shadowModel, x, y, -2.75f, 2.58f, 2.20f, 5.04f, -1.31f, 2.20f, 5.23f, -1.21f, 3.54f, -2.56f, 2.68f, 3.54f);
                this.AddShadowQuad(shadowModel, x, y, -2.56f, 2.68f, 3.54f, 5.23f, -1.21f, 3.54f, 5.04f, -1.31f, 4.84f, -2.75f, 2.58f, 4.84f);
                this.AddShadowQuad(shadowModel, x, y, -2.75f, 2.58f, 4.84f, 5.04f, -1.31f, 4.84f, 4.51f, -1.58f, 5.97f, -3.28f, 2.32f, 5.97f);
                this.AddShadowQuad(shadowModel, x, y, -3.28f, 2.32f, 5.97f, 4.51f, -1.58f, 5.97f, 3.71f, -1.98f, 6.73f, -4.08f, 1.92f, 6.73f);
                this.AddShadowQuad(shadowModel, x, y, -4.08f, 1.92f, 6.73f, 3.71f, -1.98f, 6.73f, 2.77f, -2.45f, 7.02f, -5.02f, 1.45f, 7.02f);
                this.AddShadowQuad(shadowModel, x, y, -5.02f, 1.45f, 7.02f, 2.77f, -2.45f, 7.02f, 1.82f, -2.92f, 6.73f, -5.97f, 0.97f, 6.73f);
                this.AddShadowQuad(shadowModel, x, y, -5.97f, 0.97f, 6.73f, 1.82f, -2.92f, 6.73f, 1.05f, -3.31f, 5.97f, -6.75f, 0.58f, 5.97f);
                this.AddShadowQuad(shadowModel, x, y, -6.75f, 0.58f, 5.97f, 1.05f, -3.31f, 5.97f, 0.49f, -3.59f, 4.84f, -7.30f, 0.31f, 4.84f);
                this.AddShadowQuad(shadowModel, x, y, -7.30f, 0.31f, 4.84f, 0.49f, -3.59f, 4.84f, 0.31f, -3.68f, 3.54f, -7.48f, 0.22f, 3.54f);
                this.AddShadowQuad(shadowModel, x, y, -7.48f, 0.22f, 3.54f, 0.31f, -3.68f, 3.54f, 0.49f, -3.59f, 2.20f, -7.30f, 0.31f, 2.20f);
                this.AddShadowQuad(shadowModel, x, y, -7.30f, 0.31f, 2.20f, 0.49f, -3.59f, 2.20f, 1.05f, -3.31f, 1.07f, -6.75f, 0.58f, 1.07f);
                this.AddShadowQuad(shadowModel, x, y, -6.75f, 0.58f, 1.07f, 1.05f, -3.31f, 1.07f, 1.82f, -2.92f, 0.32f, -5.97f, 0.97f, 0.32f);
                this.AddShadowQuad(shadowModel, x, y, -5.97f, 0.97f, 0.32f, 1.82f, -2.92f, 0.32f, 2.77f, -1.98f, 0.03f, -5.02f, 1.45f, 0.03f);

                // Generator
                this.AddVerticalShadowQuad(shadowModel, x, y, 5.52f, -0.73f, 7.65f, 0.34f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, 7.65f, 0.34f, 5.52f, 1.40f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, 5.52f, 1.40f, 3.38f, 0.34f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, 3.38f, 0.34f, 5.52f, -0.73f, 0, 4);
                this.AddShadowQuad(shadowModel, x, y, 4.45f, 0.99f, 0.73f, 0.35f, 3.04f, 0.73f, 0.94f, 3.34f, 1.07f, 5.04f, 1.29f, 1.07f);
                this.AddShadowQuad(shadowModel, x, y, 5.04f, 1.29f, 1.07f, 0.94f, 3.34f, 1.07f, 1.35f, 3.35f, 2.12f, 5.45f, 1.50f, 2.12f);
                this.AddShadowQuad(shadowModel, x, y, 5.45f, 1.50f, 2.12f, 1.35f, 3.35f, 2.12f, 1.03f, 3.38f, 3.25f, 5.13f, 1.33f, 3.25f);
                this.AddShadowQuad(shadowModel, x, y, 5.13f, 1.33f, 3.25f, 1.03f, 3.38f, 3.25f, 0.33f, 3.03f, 3.63f, 4.43f, 0.98f, 3.63f);
                this.AddVerticalShadowQuad(shadowModel, x, y, -0.82f, 2.44f, 1.44f, 3.57f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, 1.44f, 3.57f, 0.31f, 4.13f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, 0.31f, 4.13f, -1.95f, 3.00f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, -1.95f, 3.00f, -0.82f, 2.44f, 0, 4);

                // Chimney
                this.AddVerticalCylinderShadowQuad(shadowModel, x - 4.26f, y + 1.07f, 0.8f, 0.4f, 7f, 9.7f, 4, false);
            }
            else if (this.Direction == Direction.NE)
            {
                // Main cylinder
                this.AddShadowQuad(shadowModel, x, y, -2.89f, -2.51f, 0.03f, 4.90f, 1.38f, 0.03f, 3.96f, 1.86f, 0.32f, -3.83f, -2.04f, 0.32f);
                this.AddShadowQuad(shadowModel, x, y, -3.83f, -2.04f, 0.32f, 3.96f, 1.86f, 0.32f, 3.16f, 2.26f, 1.07f, -4.63f, -1.64f, 1.07f);
                this.AddShadowQuad(shadowModel, x, y, -4.63f, -1.64f, 1.07f, 3.16f, 2.26f, 1.07f, 2.62f, 2.52f, 2.20f, -5.17f, -1.37f, 2.20f);
                this.AddShadowQuad(shadowModel, x, y, -5.17f, -1.37f, 2.20f, 2.62f, 2.52f, 2.20f, 2.44f, 2.61f, 3.54f, -5.35f, -1.28f, 3.54f);
                this.AddShadowQuad(shadowModel, x, y, -5.35f, -1.28f, 3.54f, 2.44f, 2.61f, 3.54f, 2.62f, 2.52f, 4.84f, -5.17f, -1.37f, 4.84f);
                this.AddShadowQuad(shadowModel, x, y, -5.17f, -1.37f, 4.84f, 2.62f, 2.52f, 4.84f, 3.16f, 2.26f, 5.97f, -4.63f, -1.64f, 5.97f);
                this.AddShadowQuad(shadowModel, x, y, -4.63f, -1.64f, 5.97f, 3.16f, 2.26f, 5.97f, 3.96f, 1.86f, 6.73f, -3.83f, -2.04f, 6.73f);
                this.AddShadowQuad(shadowModel, x, y, -3.83f, -2.04f, 6.73f, 3.96f, 1.86f, 6.73f, 4.90f, 1.38f, 7.02f, -2.89f, -2.51f, 7.02f);
                this.AddShadowQuad(shadowModel, x, y, -2.89f, -2.51f, 7.02f, 4.90f, 1.38f, 7.02f, 5.84f, 0.91f, 6.73f, -1.95f, -2.98f, 6.73f);
                this.AddShadowQuad(shadowModel, x, y, -1.95f, -2.98f, 6.73f, 5.84f, 0.91f, 6.73f, 6.62f, 0.52f, 5.97f, -1.17f, -3.37f, 5.97f);
                this.AddShadowQuad(shadowModel, x, y, -1.17f, -3.37f, 5.97f, 6.62f, 0.52f, 5.97f, 7.18f, 0.25f, 4.84f, -0.62f, -3.65f, 4.84f);
                this.AddShadowQuad(shadowModel, x, y, -0.62f, -3.65f, 4.84f, 7.18f, 0.25f, 4.84f, 7.36f, 0.15f, 3.54f, -0.43f, -3.74f, 3.54f);
                this.AddShadowQuad(shadowModel, x, y, -0.43f, -3.74f, 3.54f, 7.36f, 0.15f, 3.54f, 7.18f, 0.25f, 2.20f, -0.62f, -3.65f, 2.20f);
                this.AddShadowQuad(shadowModel, x, y, -0.62f, -3.65f, 2.20f, 7.18f, 0.25f, 2.20f, 6.62f, 0.52f, 1.07f, -1.17f, -3.37f, 1.07f);
                this.AddShadowQuad(shadowModel, x, y, -1.17f, -3.37f, 1.07f, 6.62f, 0.52f, 1.07f, 5.84f, 0.91f, 0.32f, -1.95f, -2.98f, 0.32f);
                this.AddShadowQuad(shadowModel, x, y, -1.95f, -2.98f, 0.32f, 5.84f, 0.91f, 0.32f, 4.90f, 1.38f, 0.03f, -2.89f, -2.51f, 0.03f);

                // Generator
                this.AddVerticalShadowQuad(shadowModel, x, y, 1.46f, 2.76f, -0.68f, 3.82f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, -0.68f, 3.82f, -2.81f, 2.76f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, -2.81f, 2.76f, -0.68f, 1.69f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, -0.68f, 1.69f, 1.46f, 2.76f, 0, 4);
                this.AddShadowQuad(shadowModel, x, y, -1.99f, 2.22f, 0.73f, -6.09f, 0.17f, 0.73f, -6.68f, 0.47f, 1.07f, -2.58f, 2.52f, 1.07f);
                this.AddShadowQuad(shadowModel, x, y, -2.28f, 2.52f, 1.07f, -6.68f, 0.47f, 1.07f, -7.09f, 0.68f, 2.12f, -2.99f, 2.73f, 2.12f);
                this.AddShadowQuad(shadowModel, x, y, -2.99f, 2.73f, 2.12f, -7.09f, 0.68f, 2.12f, -6.77f, 0.51f, 3.25f, -2.67f, 2.56f, 3.25f);
                this.AddShadowQuad(shadowModel, x, y, -2.67f, 2.56f, 3.25f, -6.77f, 0.51f, 3.25f, -6.07f, 0.16f, 3.63f, -1.97f, 2.21f, 3.63f);
                this.AddVerticalShadowQuad(shadowModel, x, y, -4.88f, -0.41f, -7.14f, 0.72f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, -7.14f, 0.72f, -8.26f, 0.15f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, -8.26f, 0.15f, -6.01f, -0.97f, 0, 4);
                this.AddVerticalShadowQuad(shadowModel, x, y, -6.01f, -0.97f, -4.88f, -0.41f, 0, 4);

                // Chimney
                this.AddVerticalCylinderShadowQuad(shadowModel, x - 2.15f, y - 2.14f, 0.8f, 0.4f, 7f, 9.7f, 4, false);
            }

            this.ShadowModel.SetModel(shadowModel, this);
        }
    }
}

