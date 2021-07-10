namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using ProtoBuf;
    using WorldInterfaces;
    using Shared;

    [ProtoContract]
    public class ElectricFurnace : FactoryBuilding, IEnergyConsumer, IRotatableThing, IRepairableThing, IResourceProviderBuilding, IResourceConsumerBuilding
    {
        private int animationTimer;
        private bool animationPhase;

        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        public ElectricFurnace() : base()
        {
        }

        public ElectricFurnace(ISmallTile tile, Direction direction) : base(ThingType.ElectricFurnace, tile, 1)
        {
            this.Direction = direction;
        }

        protected override void Init()
        {
            this.framesToInitialise = Constants.ElectricFurnaceFramesToInitialise;
            this.framesToProcess = Constants.ElectricFurnaceFramesToProcess;
            this.framesToPauseResume = Constants.ElectricFurnaceFramesToPauseResume;
            this.energyPerHour = Energy.FromKwH(Constants.ElectricFurnaceEnergyUse);
            this.energyPerFrame = energyPerHour / Constants.FramesPerHour;
            this.capacitorSize = Constants.ElectricFurnaceEnergyStore;
            this.producedItemType = ItemType.Metal;
            this.consumedItemType = ItemType.IronOre;
            base.Init();
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{base.GetTextureName()}_{this.Direction.ToString()}";
        }

        protected override void UpdateAnimationFrame()
        {
            if (this.IsRecycling)
            {
                this.AnimationFrame = 1;
                return;
            }

            if (this.FactoryStatus == FactoryStatus.Broken)
            {
                this.AnimationFrame = World.WorldTime.Minute % 2 == 0 ? 12 : 1;
                return;
            }

            if (this.FactoryStatus == FactoryStatus.Standby || this.FactoryStatus == FactoryStatus.Paused || this.FactoryStatus == FactoryStatus.Initialising)
            {
                this.AnimationFrame = 11;
                return;
            }

            this.animationTimer = this.animationTimer < 8 ? this.animationTimer + 1 : 0;

            bool isInProgress = this.FactoryStatus.In(FactoryStatus.InProgress, FactoryStatus.Pausing, FactoryStatus.Resuming);
            if (isInProgress && (this.AnimationFrame == 1 || this.AnimationFrame > 10))
            {
                this.AnimationFrame = 10;
                this.animationPhase = false;
            }
            else if (!isInProgress && this.AnimationFrame == 1) return;

            if (this.animationTimer > 0) return;

            if (!isInProgress)
            {
                this.animationPhase = true;
                if (this.AnimationFrame >= 10)
                {
                    this.AnimationFrame = 1;
                    return;
                }
            }

            if (this.animationPhase == true)
            {
                this.AnimationFrame++;
                if (this.AnimationFrame == 10)
                {
                    this.animationPhase = false;
                }
            }
            else
            {
                this.AnimationFrame--;
                if (this.AnimationFrame == 2)
                {
                    this.animationPhase = true;
                }
            }
        }

        protected override void TryDistribute()
        {
            var itemCount = this.OutputItemCount;
            base.TryDistribute();
            if (this.OutputItemCount < itemCount) WorldStats.Increment(WorldStatKeys.MetalSmeltedElectricFurnace, itemCount - this.OutputItemCount);
        }

        public override void UpdateShadowModel()
        {
            var x = this.MainTile.CentrePosition.X;
            var y = this.MainTile.CentrePosition.Y;

            var model = new List<Vector3>();

            // Main Cylinder
            this.AddVerticalCylinderShadowQuad(model, x, y, 6, 3, 1, 5, 16, true);

            // Post and top connector
            if (this.Direction == Direction.SE)
            {
                this.AddShadowQuad(model, x, y, -1.00f, -4.8f, 0, 1.00f, -4.8f, 0, 0.71f, -4.72f, 7.9f, -0.71f, -4.72f, 7.9f);
                this.AddShadowQuad(model, x, y, 1.00f, -4.8f, 0, 1.00f, -4.8f, 0, 0.71f, -4.76f, 7.9f, 0.71f, -4.72f, 7.9f);
                this.AddShadowQuad(model, x, y, 1.00f, -4.3f, 0, -1.00f, -4.3f, 0, -0.71f, -4.36f, 7.9f, 0.71f, -4.36f, 7.9f);
                this.AddShadowQuad(model, x, y, -1.00f, -4.3f, 0, -1.00f, -4.8f, 0, -0.71f, -4.72f, 7.9f, -0.71f, -4.36f, 7.9f);
                this.AddHorizontalShadowQuad(model, x, y, 0.15f, -4.67f, -0.15f, -4.67f, -0.15f, -0.17f, 0.15f, -0.17f, 7.5f);
                this.AddVerticalShadowQuad(model, x, y, 0, -4.67f, 0, 0.97f, 7.24f, 7.5f);
            }
            else if (this.Direction == Direction.SW)
            {
                this.AddShadowQuad(model, x, y, 8.99f, -0.8f, 0, 8.99f, 0.2f, 0, 8.84f, 0.04f, 7.9f, 8.84f, -0.66f, 7.9f);
                this.AddShadowQuad(model, x, y, 8.99f, 0.2f, 0, 7.98f, .2f, 0, 8.12f, 0.04f, 7.9f, 8.84f, 0.04f, 7.9f);
                this.AddShadowQuad(model, x, y, 7.98f, 0.2f, 0, 7.98f, -0.8f, 0, 8.12f, -0.66f, 7.9f, 8.12f, 0.04f, 7.9f);
                this.AddShadowQuad(model, x, y, 7.98f, -0.8f, 0, 8.99f, -0.8f, 0, 8.84f, -0.66f, 7.9f, 8.12f, -0.66f, 7.9f);
                this.AddHorizontalShadowQuad(model, x, y, 8.75f, -0.23f, 8.57f, -0.37f, -0.26f, -0.37f, 0.26f, -0.23f, 7.5f);
                this.AddVerticalShadowQuad(model, x, y, 8.74f, -0.3f, -2.55f, -0.3f, 7.24f, 7.5f);
            }
            else if (this.Direction == Direction.NW)
            {
                this.AddShadowQuad(model, x, y, -1.00f, 4.2f, 0, 1.00f, 4.2f, 0, 0.71f, 4.12f, 7.9f, -0.71f, 4.12f, 7.9f);
                this.AddShadowQuad(model, x, y, 1.00f, 4.2f, 0, 1.00f, 3.7f, 0, 0.71f, 3.76f, 7.9f, 0.71f, 4.1f, 7.9f);
                this.AddShadowQuad(model, x, y, 1.00f, 3.7f, 0, -1.00f, 3.7f, 0, -0.71f, 3.76f, 7.9f, 0.71f, 3.7f, 7.9f);
                this.AddShadowQuad(model, x, y, -1.00f, 3.7f, 0, -1.00f, 4.2f, 0, -0.71f, 4.12f, 7.9f, -0.71f, 3.76f, 7.9f);
                this.AddHorizontalShadowQuad(model, x, y, 0.15f, 4.07f, -0.15f, 4.07f, -0.15f, -0.43f, 0.15f, -0.43f, 7.5f);
                this.AddVerticalShadowQuad(model, x, y, 0, 4.07f, 0, -1.57f, 7.24f, 7.5f);
            }
            else// if (this.Direction == Direction.NE)
            {
                this.AddShadowQuad(model, x, y, -8.99f, -0.8f, 0, -8.99f, 0.2f, 0, -8.84f, 0.04f, 7.9f, -8.84f, -0.66f, 7.9f);
                this.AddShadowQuad(model, x, y, -8.99f, 0.2f, 0, -7.98f, .2f, 0, -8.12f, 0.04f, 7.9f, -8.84f, 0.04f, 7.9f);
                this.AddShadowQuad(model, x, y, -7.98f, 0.2f, 0, -7.98f, -0.8f, 0, -8.12f, -0.66f, 7.9f, -8.12f, 0.04f, 7.9f);
                this.AddShadowQuad(model, x, y, -7.98f, -0.8f, 0, -8.99f, -0.8f, 0, -8.84f, -0.66f, 7.9f, -8.12f, -0.66f, 7.9f);
                this.AddHorizontalShadowQuad(model, x, y, -8.75f, -0.23f, -8.57f, -0.37f, 0.26f, -0.37f, -0.26f, -0.23f, 7.5f);
                this.AddVerticalShadowQuad(model, x, y, -8.74f, -0.3f, 2.55f, -0.3f, 7.24f, 7.5f);
            }

            // Pipe - N
            if (this.Direction != Direction.SE)
            {
                this.AddShadowQuad(model, x, y, 0.00f, -4.68f, 0.00f, -0.81f, -4.31f, 0.00f, -0.85f, -4.33f, 2.00f, 0.00f, -4.72f, 2.20f);
                this.AddShadowQuad(model, x, y, -0.81f, -4.31f, 0.00f, 0.00f, -3.91f, 0.00f, -0.07f, -3.94f, 1.90f, -0.85f, -4.33f, 2.00f);
                this.AddShadowQuad(model, x, y, 0.00f, -3.91f, 0.00f, 0.78f, -4.29f, 0.00f, 0.78f, -4.29f, 2.00f, -0.07f, -3.94f, 1.90f);
                this.AddShadowQuad(model, x, y, 0.78f, -4.29f, 0.00f, 0.00f, -4.68f, 0.00f, 0.00f, -4.72f, 2.20f, 0.78f, -4.29f, 2.00f);
                this.AddShadowQuad(model, x, y, 0.00f, -4.72f, 2.20f, -0.85f, -4.33f, 2.00f, -0.85f, -3.98f, 3.10f, 0.00f, -4.19f, 3.80f);
                this.AddShadowQuad(model, x, y, -0.85f, -4.33f, 2.00f, -0.07f, -3.94f, 1.90f, -0.07f, -3.80f, 2.50f, -0.85f, -3.98f, 3.10f);
                this.AddShadowQuad(model, x, y, -0.07f, -3.94f, 1.90f, 0.78f, -4.29f, 2.00f, 0.71f, -3.98f, 3.10f, -0.85f, -3.98f, 3.10f);
                this.AddShadowQuad(model, x, y, 0.78f, -4.29f, 2.00f, 0.00f, -4.72f, 2.20f, 0.00f, -4.19f, 3.80f, -0.85f, -3.98f, 3.10f);
                this.AddShadowQuad(model, x, y, 0.00f, -4.19f, 3.80f, -0.85f, -3.98f, 3.10f, -0.92f, -3.38f, 3.50f, -0.07f, -3.45f, 4.30f);
                this.AddShadowQuad(model, x, y, -0.85f, -3.98f, 3.10f, -0.07f, -3.80f, 2.50f, -0.07f, -3.30f, 2.80f, -0.92f, -3.38f, 3.50f);
                this.AddShadowQuad(model, x, y, -0.07f, -3.80f, 2.50f, 0.71f, -3.98f, 3.10f, 0.78f, -3.38f, 3.50f, -0.07f, -3.30f, 2.80f);
                this.AddShadowQuad(model, x, y, 0.71f, -3.98f, 3.10f, 0.00f, -4.19f, 3.80f, -0.07f, -3.45f, 4.30f, 0.78f, -3.38f, 3.50f);
            }

            // Pipe - E
            if (this.Direction != Direction.SW)
            {
                this.AddShadowQuad(model, x, y, 8.77f, -0.30f, 0.00f, 8.02f, -0.71f, 0.00f, 8.06f, -0.72f, 2.00f, 8.84f, -0.30f, 2.20f);
                this.AddShadowQuad(model, x, y, 8.02f, -0.71f, 0.00f, 7.21f, -0.30f, 0.00f, 7.28f, -0.34f, 1.90f, 8.06f, -0.72f, 2.00f);
                this.AddShadowQuad(model, x, y, 7.21f, -0.30f, 0.00f, 7.99f, 0.09f, 0.00f, 7.99f, 0.09f, 2.00f, 7.28f, -0.34f, 1.90f);
                this.AddShadowQuad(model, x, y, 7.99f, 0.09f, 0.00f, 8.77f, -0.30f, 0.00f, 8.84f, -0.30f, 2.20f, 7.99f, 0.09f, 2.00f);
                this.AddShadowQuad(model, x, y, 8.84f, -0.30f, 2.20f, 8.06f, -0.72f, 2.00f, 7.35f, -0.72f, 3.10f, 7.78f, -0.30f, 3.80f);
                this.AddShadowQuad(model, x, y, 8.06f, -0.72f, 2.00f, 7.28f, -0.34f, 1.90f, 7.00f, -0.34f, 2.50f, 7.35f, -0.72f, 3.10f);
                this.AddShadowQuad(model, x, y, 7.28f, -0.34f, 1.90f, 7.99f, 0.09f, 2.00f, 7.35f, 0.05f, 3.10f, 7.35f, -0.72f, 3.10f);
                this.AddShadowQuad(model, x, y, 7.99f, 0.09f, 2.00f, 8.84f, -0.30f, 2.20f, 7.78f, -0.30f, 3.80f, 7.35f, -0.72f, 3.10f);
                this.AddShadowQuad(model, x, y, 7.78f, -0.30f, 3.80f, 7.35f, -0.72f, 3.10f, 6.15f, -0.76f, 3.50f, 6.29f, -0.34f, 4.30f);
                this.AddShadowQuad(model, x, y, 7.35f, -0.72f, 3.10f, 7.00f, -0.34f, 2.50f, 6.01f, -0.34f, 2.80f, 6.15f, -0.76f, 3.50f);
                this.AddShadowQuad(model, x, y, 7.00f, -0.34f, 2.50f, 7.35f, 0.05f, 3.10f, 6.15f, 0.09f, 3.50f, 6.01f, -0.34f, 2.80f);
                this.AddShadowQuad(model, x, y, 7.35f, 0.05f, 3.10f, 7.78f, -0.30f, 3.80f, 6.29f, -0.34f, 4.30f, 6.15f, 0.09f, 3.50f);
            }

            // Pipe - S
            if (this.Direction != Direction.NW)
            {
                this.AddShadowQuad(model, x, y, 0.00f, 4.08f, 0.00f, -0.81f, 3.71f, 0.00f, -0.85f, 3.73f, 2.00f, 0.00f, 4.12f, 2.20f);
                this.AddShadowQuad(model, x, y, -0.81f, 3.71f, 0.00f, 0.00f, 3.31f, 0.00f, -0.07f, 3.34f, 1.90f, -0.85f, 3.73f, 2.00f);
                this.AddShadowQuad(model, x, y, 0.00f, 3.31f, 0.00f, 0.78f, 3.69f, 0.00f, 0.78f, 3.69f, 2.00f, -0.07f, 3.34f, 1.90f);
                this.AddShadowQuad(model, x, y, 0.78f, 3.69f, 0.00f, 0.00f, 4.08f, 0.00f, 0.00f, 4.12f, 2.20f, 0.78f, 3.69f, 2.00f);
                this.AddShadowQuad(model, x, y, 0.00f, 4.12f, 2.20f, -0.85f, 3.73f, 2.00f, -0.85f, 3.38f, 3.10f, 0.00f, 3.59f, 3.80f);
                this.AddShadowQuad(model, x, y, -0.85f, 3.73f, 2.00f, -0.07f, 3.34f, 1.90f, -0.07f, 3.20f, 2.50f, -0.85f, 3.38f, 3.10f);
                this.AddShadowQuad(model, x, y, -0.07f, 3.34f, 1.90f, 0.78f, 3.69f, 2.00f, 0.71f, 3.38f, 3.10f, -0.85f, 3.38f, 3.10f);
                this.AddShadowQuad(model, x, y, 0.78f, 3.69f, 2.00f, 0.00f, 4.12f, 2.20f, 0.00f, 3.59f, 3.80f, -0.85f, 3.38f, 3.10f);
                this.AddShadowQuad(model, x, y, 0.00f, 3.59f, 3.80f, -0.85f, 3.38f, 3.10f, -0.92f, 2.78f, 3.50f, -0.07f, 2.85f, 4.30f);
                this.AddShadowQuad(model, x, y, -0.85f, 3.38f, 3.10f, -0.07f, 3.20f, 2.50f, -0.07f, 2.70f, 2.80f, -0.92f, 2.78f, 3.50f);
                this.AddShadowQuad(model, x, y, -0.07f, 3.20f, 2.50f, 0.71f, 3.38f, 3.10f, 0.78f, 2.78f, 3.50f, -0.07f, 2.70f, 2.80f);
                this.AddShadowQuad(model, x, y, 0.71f, 3.38f, 3.10f, 0.00f, 3.59f, 3.80f, -0.07f, 2.85f, 4.30f, 0.78f, 2.78f, 3.50f);
            }

            // Pipe - W
            if (this.Direction != Direction.NE)
            {
                this.AddShadowQuad(model, x, y, -8.77f, -0.30f, 0.00f, -8.02f, -0.71f, 0.00f, -8.06f, -0.72f, 2.00f, -8.84f, -0.30f, 2.20f);
                this.AddShadowQuad(model, x, y, -8.02f, -0.71f, 0.00f, -7.21f, -0.30f, 0.00f, -7.28f, -0.34f, 1.90f, -8.06f, -0.72f, 2.00f);
                this.AddShadowQuad(model, x, y, -7.21f, -0.30f, 0.00f, -7.99f, 0.09f, 0.00f, -7.99f, 0.09f, 2.00f, -7.28f, -0.34f, 1.90f);
                this.AddShadowQuad(model, x, y, -7.99f, 0.09f, 0.00f, -8.77f, -0.30f, 0.00f, -8.84f, -0.30f, 2.20f, -7.99f, 0.09f, 2.00f);
                this.AddShadowQuad(model, x, y, -8.84f, -0.30f, 2.20f, -8.06f, -0.72f, 2.00f, -7.35f, -0.72f, 3.10f, -7.78f, -0.30f, 3.80f);
                this.AddShadowQuad(model, x, y, -8.06f, -0.72f, 2.00f, -7.28f, -0.34f, 1.90f, -7.00f, -0.34f, 2.50f, -7.35f, -0.72f, 3.10f);
                this.AddShadowQuad(model, x, y, -7.28f, -0.34f, 1.90f, -7.99f, 0.09f, 2.00f, -7.35f, 0.05f, 3.10f, -7.35f, -0.72f, 3.10f);
                this.AddShadowQuad(model, x, y, -7.99f, 0.09f, 2.00f, -8.84f, -0.30f, 2.20f, -7.78f, -0.30f, 3.80f, -7.35f, -0.72f, 3.10f);
                this.AddShadowQuad(model, x, y, -7.78f, -0.30f, 3.80f, -7.35f, -0.72f, 3.10f, -6.15f, -0.76f, 3.50f, -6.29f, -0.34f, 4.30f);
                this.AddShadowQuad(model, x, y, -7.35f, -0.72f, 3.10f, -7.00f, -0.34f, 2.50f, -6.01f, -0.34f, 2.80f, -6.15f, -0.76f, 3.50f);
                this.AddShadowQuad(model, x, y, -7.00f, -0.34f, 2.50f, -7.35f, 0.05f, 3.10f, -6.15f, 0.09f, 3.50f, -6.01f, -0.34f, 2.80f);
                this.AddShadowQuad(model, x, y, -7.35f, 0.05f, 3.10f, -7.78f, -0.30f, 3.80f, -6.29f, -0.34f, 4.30f, -6.15f, 0.09f, 3.50f);
            }

            this.ShadowModel.SetModel(model, this);
        }
    }
}
