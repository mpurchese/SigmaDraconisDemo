namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using Shared;
    using World;
    using World.Particles;
    using WorldControllers;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionDeconstruct : ActionBase
    {
        public IRecyclableThing Target { get; private set; }

        [ProtoMember(1)]
        private int targetId;

        [ProtoMember(2)]
        private int? jobId;

        // Deserialisation ctor
        protected ActionDeconstruct() { }

        public ActionDeconstruct(IColonist colonist, IRecyclableThing target) : base(colonist)
        {
            this.Target = target;
            this.targetId = target.Id;
            colonist.IsMoving = false;
        }

        public override void AfterDeserialization()
        {
            this.Target = World.GetThing(this.targetId) as IRecyclableThing;
            base.AfterDeserialization();

            if (this.Target == null) this.IsFinished = true;
        }

        public override void Update()
        {
            this.OpenDoorIfExists();

            if (!jobId.HasValue && this.Colonist.RaisedArmsFrame < 18)
            {
                this.Colonist.IsWorking = true;
                base.Update();
                return;
            }

            // Mood affects work speed
            var frames = this.Target.RecycleTime;

            if (!jobId.HasValue) this.jobId = ResourceDeconstructionController.Add(this.Colonist, this.Target, frames);

            if (ResourceDeconstructionController.IsFinished(this.jobId.Value))
            {
                if (!MicrobotParticleController.ActiveColonists.Contains(this.Colonist.Id))
                {
                    this.Colonist.IsWorking = false;
                    if (this.Colonist.RaisedArmsFrame == 0) this.IsFinished = true;
                }
            }
            else
            {
                (this.Target as IColonistInteractive).AssignColonist(this.Colonist.Id, this.Colonist.MainTile.Index);
                this.Colonist.IsWorking = true;
                this.DoParticles();

                // Remove the job icon
                if (World.ResourcesForDeconstruction.ContainsKey(this.Target.Id))
                {
                    World.ResourcesForDeconstruction.Remove(this.Target.Id);
                    EventManager.EnqueueWorldPropertyChangeEvent(this.Target.Id, nameof(World.ResourcesForDeconstruction), this.Target.MainTile.Row, this.Target.ThingType);
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
            for (int i = 0; i < 8; i++)
            {
                var tile = this.Target.AllTiles[Rand.Next(this.Target.AllTiles.Count)];
                var offsetX = (float)((Rand.NextDouble() - 0.5) * 10f) + tile.CentrePosition.X - renderTile.CentrePosition.X;
                var offsetY = (float)((Rand.NextDouble() - 0.5) * 5f) + tile.CentrePosition.Y - renderTile.CentrePosition.Y;

                if (this.Target is IRenderOffsettable r)
                {
                    // Some things like grass may not be centered on the tile
                    offsetX += r.RenderPositionOffset.X;
                    offsetY += r.RenderPositionOffset.Y;
                }

                if (this.Target?.RenderAlpha >= 0.999f || (this.Target?.RenderAlpha >= 0.5f && Rand.NextDouble() > 0.5))
                {
                    MicrobotParticleController.AddParticle(renderTile, toolOffset.X, toolOffset.Y, z, offsetX, offsetY, 0f, this.Colonist.Id, false);
                }
                else
                {
                    MicrobotParticleController.AddParticle(renderTile, offsetX, offsetY, 0f, toolOffset.X, toolOffset.Y, z, this.Colonist.Id, true);
                }
            }
        }
    }
}
