namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using Shared;
    using World;
    using World.Particles;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionFarmPlant : ActionBase
    {
        public IPlanter Planter { get; private set; }

        [ProtoMember(1)]
        private int counter = 0;

        [ProtoMember(2)]
        private readonly int planterId;

        // Deserialisation ctor
        protected ActionFarmPlant() { }

        public ActionFarmPlant(IColonist colonist, IPlanter planter) : base(colonist)
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
                this.Colonist.IsWorking = true;
                if (this.Planter.JobProgress < 0.99f) this.DoParticles();
                this.Planter.DoJob(this.Colonist.GetWorkRate());
            }
            else if (this.Planter?.IsReady == true && this.Planter.PlanterStatus != PlanterStatus.WaitingToHarvest && (this.Planter.PlanterStatus == PlanterStatus.Dead || this.Planter.RemoveCrop))
            {
                this.Colonist.IsWorking = true;
                this.Planter.DoJob(this.Colonist.GetWorkRate());
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

        private void DoParticles()
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
            for (int i = 0; i < 8; i++)
            {
                var offsetX = (float)((Rand.NextDouble() - 0.5) * 10f) + tile.CentrePosition.X - renderTile.CentrePosition.X;
                var offsetY = (float)((Rand.NextDouble() - 0.5) * 5f) + tile.CentrePosition.Y - renderTile.CentrePosition.Y;

                MicrobotParticleController.AddParticle(renderTile, toolOffset.X, toolOffset.Y, z, offsetX, offsetY, 0.4f, this.Colonist.Id, false, 2, false);
            }
        }
    }
}
