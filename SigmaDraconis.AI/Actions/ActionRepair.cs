namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using Shared;
    using World;
    using World.Particles;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionRepair : ActionBase
    {
        public IRepairableThing Target { get; private set; }

        [ProtoMember(2)]
        private readonly int targetId;

        // Deserialisation ctor
        protected ActionRepair() { }

        public ActionRepair(IColonist colonist, IRepairableThing target) : base(colonist)
        {
            this.Target = target;
            this.targetId = target.Id;
        }

        public override void AfterDeserialization()
        {
            this.Target = World.GetThing(this.targetId) as IRepairableThing;
            base.AfterDeserialization();
        }

        public override void Update()
        {
            this.Colonist.IsWorking = true;
            this.OpenDoorIfExists();

            if (!(this.Target is IRepairableThing rt))
            {
                this.Colonist.IsWorking = false;
                this.IsFinished = true;
                return;
            }

            rt.AssignColonistForRepair(this.Colonist.Id, this.Colonist.MainTile.Index);
            if (rt.MaintenanceLevel > 0.998 && this.Colonist.RaisedArmsFrame < 18)
            {
                this.Colonist.IsWorking = false;
            }
            else if (this.Colonist.RaisedArmsFrame == 18)
            {
                this.DoParticles();
                if (this.Target.DoRepair(this.Colonist.GetWorkRate())) this.Colonist.IsWorking = false;
            }

            if (!this.Colonist.IsWorking && this.Colonist.RaisedArmsFrame == 0) this.IsFinished = true;

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
            for (int i = 0; i < 8; i++)
            {
                var tile = this.Target.AllTiles[Rand.Next(this.Target.AllTiles.Count)];
                var offsetX = (float)((Rand.NextDouble() - 0.5) * 10f) + tile.CentrePosition.X - renderTile.CentrePosition.X;
                var offsetY = (float)((Rand.NextDouble() - 0.5) * 5f) + tile.CentrePosition.Y - renderTile.CentrePosition.Y;

                MicrobotParticleController.AddParticle(renderTile, toolOffset.X, toolOffset.Y, z, offsetX, offsetY, 0f, this.Colonist.Id, false, 1);
            }
        }
    }
}
