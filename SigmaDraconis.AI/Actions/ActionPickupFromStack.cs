namespace SigmaDraconis.AI
{
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using World;
    using World.Particles;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionPickupFromStack : ActionBase
    {
        [ProtoMember(1)]
        private int counter = 0;

        public IResourceStack Stack { get; private set; }

        [ProtoMember(2)]
        private readonly int stackId;

        // Deserialisation ctor
        protected ActionPickupFromStack() { }

        public ActionPickupFromStack(IColonist colonist, IResourceStack stack) : base(colonist)
        {
            this.Stack = stack;
            this.stackId = stack.Id;
        }

        public override void AfterDeserialization()
        {
            this.Stack = World.GetThing(this.stackId) as IResourceStack;
            base.AfterDeserialization();
        }

        public override void Update()
        {
            if (this.Stack == null)
            {
                // For some reason can happen after load
                this.IsFailed = true;
                this.IsFinished = true;
                return;
            }

            counter++;
            this.Colonist.IsWorking = true;
            this.OpenDoorIfExists();

            if (counter >= 60)
            {
                // Finished
                if (!MicrobotParticleController.ActiveColonists.Contains(this.Colonist.Id))
                {
                    this.Colonist.IsWorking = false;
                    if (this.Colonist.RaisedArmsFrame == 0) this.IsFinished = true;
                }
            }
            else if (counter > 30)
            {
                // Particle effect
                this.DoParticles(true);
            }
            else if (counter == 30)
            {
                // Take the resource
                if (this.Stack?.ItemCount > 0 && this.Colonist.CarriedItemTypeBack == ItemType.None)
                {
                    this.Stack.TakeItem();
                    this.Colonist.CarriedItemTypeBack = this.Stack.ItemType;
                    ResourceStackingController.JobInTransit(this.Colonist);
                    EventManager.EnqueueWorldPropertyChangeEvent(this.Colonist.Id, nameof(IColonist.CarriedItemTypeBack), this.Colonist.MainTile.Row, ThingType.Colonist);
                    if (this.Stack.ItemCount == 0 && !this.Stack.MainTile.ThingsPrimary.OfType<IStackingArea>().Any())
                    {
                        World.RemoveThing(this.Stack);
                    }
                }
            }
            else if (this.Colonist.RaisedArmsFrame == 18)
            {
                // Particle effect
                this.DoParticles(false);
            }

            base.Update();
        }

        private void DoParticles(bool incoming)
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
                var tile = this.Stack.MainTile;
                var offsetX = ((Rand.NextFloat() - 0.5f) * 10f) + tile.CentrePosition.X - renderTile.CentrePosition.X;
                var offsetY = ((Rand.NextFloat() - 0.5f) * 5f) + tile.CentrePosition.Y - renderTile.CentrePosition.Y;

                if (incoming)
                {
                    MicrobotParticleController.AddParticle(renderTile, offsetX, offsetY, 0f, toolOffset.X, toolOffset.Y, z, this.Colonist.Id, true);
                }
                else
                {
                    MicrobotParticleController.AddParticle(renderTile, toolOffset.X, toolOffset.Y, z, offsetX, offsetY, 0f, this.Colonist.Id, false);
                }
            }
        }
    }
}
