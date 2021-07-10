namespace SigmaDraconis.AI
{
    using Draconis.Shared;
    using ProtoBuf;
    using System.Collections.Generic;
    using System.Linq;
    using Shared;
    using World;
    using World.Particles;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionFarmHarvest : ActionBase
    {
        public IPlanter Planter { get; private set; }

        [ProtoMember(1)]
        private int counter = 0;

        [ProtoMember(2)]
        private readonly int planterId;

        private static readonly Dictionary<int, int> colourIndexes = new Dictionary<int, int> { { 1, 3 }, { 2, 1 }, { 3, 4 }, { 4, 0 }, { 5, 5 }, { 6, 8 } };

        // Deserialisation ctor
        protected ActionFarmHarvest() { }

        public ActionFarmHarvest(IColonist colonist, IPlanter planter) : base(colonist)
        {
            this.Planter = planter;
            this.planterId = planter.Id;
            this.ApplyTileBlock();
        }

        public override void AfterDeserialization()
        {
            this.Planter = World.GetThing(this.planterId) as IPlanter;
            base.AfterDeserialization();
            this.ApplyTileBlock();
        }

        public override void Update()
        {
            this.counter++;
            this.OpenDoorIfExists();
            if (this.counter < 30 && this.Colonist.RaisedArmsFrame < 18) return;

            if (this.Planter?.IsReady == true && this.Planter.PlanterStatus == PlanterStatus.WaitingForSeeds && (this.Colonist.WorkPriorities[ColonistPriority.FarmPlant] > 0 || this.Planter.JobProgress > 0))
            {
                // Replanting
                this.Colonist.IsWorking = true;
                if (this.Planter.JobProgress < 0.99f) this.DoParticles(true);
                this.Planter.DoJob(this.Colonist.GetWorkRate());
            }
            else if (this.Planter?.IsReady == true && (this.Planter.PlanterStatus == PlanterStatus.Dead || this.Planter.RemoveCrop || (this.Planter.PlanterStatus == PlanterStatus.WaitingToHarvest
                && !this.Colonist.CarriedCropType.HasValue
                && World.GetThings<ICooker>(ThingType.Cooker).Any(t => !t.FactoryStatus.In(FactoryStatus.Offline, FactoryStatus.NoPower, FactoryStatus.WaitingToDistribute)))))
            {
                if (this.Planter.JobProgress < 0.99f) this.DoParticles(false);

                // Only harvest if there is a cooker available
                this.Colonist.IsWorking = true;
                var cropType = this.Planter.CurrentCropTypeId;
                if (this.Planter.DoJob(this.Colonist.GetWorkRate()))  // Only returns true if a crop was harvested
                {
                    this.Colonist.CarriedItemTypeBack = ItemType.Crop;
                    this.Colonist.CarriedCropType = cropType;

                    // For variety farmer achievement
                    switch (cropType)
                    {
                        case 1: WorldStats.Increment(WorldStatKeys.CropsHarvested1); break;
                        case 2: WorldStats.Increment(WorldStatKeys.CropsHarvested2); break;
                        case 3: WorldStats.Increment(WorldStatKeys.CropsHarvested3); break;
                        case 4: WorldStats.Increment(WorldStatKeys.CropsHarvested4); break;
                        case 5: WorldStats.Increment(WorldStatKeys.CropsHarvested5); break;
                    }

                    WorldStats.Increment(WorldStatKeys.CropsHarvested);
                    EventManager.EnqueueWorldPropertyChangeEvent(this.Colonist.Id, nameof(IColonist.CarriedItemTypeBack), null, cropType, this.Colonist.MainTile.Row, ThingType.Colonist);
                }
            }
            else if (!MicrobotParticleController.ActiveColonists.Contains(this.Colonist.Id))
            {
                this.Colonist.IsWorking = false;
                if (this.Colonist.RaisedArmsFrame == 0)
                {
                    this.IsFinished = true;
                    this.ReleaseTileBlock();
                }
            }

            base.Update();
        }

        private void DoParticles(bool isReplanting)
        {
            var renderTile = this.Colonist.MainTile;
            var toolOffset = this.GetToolOffset() + this.Colonist.PositionOffset;

            // Render in a different tile if facing N, to fix render order problem
            if (this.Colonist.FacingDirection != Direction.E && this.Colonist.FacingDirection != Direction.W)
            {
                var nextTile = renderTile.GetTileToDirection(this.Colonist.FacingDirection);
                if (nextTile != null)
                {
                    renderTile = nextTile;
                    toolOffset.X += this.Colonist.MainTile.CentrePosition.X - renderTile.CentrePosition.X;
                    toolOffset.Y += this.Colonist.MainTile.CentrePosition.Y - renderTile.CentrePosition.Y;
                }
            }

            var z = 4.4f;
            var tile = this.Planter.MainTile;
            var offsetX = (float)((Rand.NextDouble() - 0.5) * 10f) + tile.CentrePosition.X - renderTile.CentrePosition.X;
            var offsetY = (float)((Rand.NextDouble() - 0.5) * 5f) + tile.CentrePosition.Y - renderTile.CentrePosition.Y;
            if (isReplanting)
            {
                for (int i = 0; i < 8; i++)
                {
                    MicrobotParticleController.AddParticle(renderTile, toolOffset.X, toolOffset.Y, z, offsetX, offsetY, 0.4f, this.Colonist.Id, false, 2, false);
                }
            }
            else
            {
                var colourIndex = colourIndexes.ContainsKey(this.Planter.CurrentCropTypeId) ? colourIndexes[this.Planter.CurrentCropTypeId] : 0;
                for (int i = 0; i < 8; i++)
                {
                    MicrobotParticleController.AddParticle(renderTile, offsetX, offsetY, 0f, toolOffset.X, toolOffset.Y, z, this.Colonist.Id, true, colourIndex, false);
                }
            }
        }
    }
}
