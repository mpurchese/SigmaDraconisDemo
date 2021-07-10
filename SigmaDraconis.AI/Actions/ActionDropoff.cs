namespace SigmaDraconis.AI
{
    using Draconis.Shared;
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using World;
    using World.Particles;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionDropoff : ActionBase
    {
        [ProtoMember(1)]
        private int counter = 0;

        public IThing Target { get; private set; }

        [ProtoMember(2)]
        private readonly int targetId;

        [ProtoMember(3)]
        private int processorWaitCounter = 0;

        // Deserialisation ctor
        protected ActionDropoff() { }

        public ActionDropoff(IColonist colonist, IThing target) : base(colonist)
        {
            this.Target = target;
            this.targetId = target.Id;
        }

        public override void AfterDeserialization()
        {
            this.Target = World.GetThing(this.targetId) as IThing;
            base.AfterDeserialization();
        }

        public override void Update()
        {
            counter++;
            this.Colonist.IsWorking = true;
            this.Colonist.ActivityType = ColonistActivityType.HaulDropoff;
            this.OpenDoorIfExists();

            if (this.Target is IResourceProcessor processor)
            {
                if (this.Colonist.RaisedArmsFrame > 15 && this.Colonist.CarriedItemTypeBack != ItemType.None && this.processorWaitCounter < Constants.ResourceProcessorFramesToProcess)
                {
                    if (processor.CanAddResource(this.Colonist.Id))
                    {
                        processor.AddResource(this.Colonist.CarriedItemTypeBack);
                        this.Colonist.CarriedItemTypeBack = ItemType.None;
                        EventManager.EnqueueWorldPropertyChangeEvent(this.Colonist.Id, nameof(IColonist.CarriedItemTypeBack), this.Colonist.MainTile.Row, ThingType.Colonist);
                    }
                    else
                    {
                        // Either the processor is running another job, or the network is full.  We'll wait a short while.
                        this.counter--;
                        this.processorWaitCounter += processor.FactoryStatus == FactoryStatus.WaitingToDistribute ? 10 : 1;
                    }
                }
                else if (counter >= Constants.ResourceProcessorFramesToProcess * 1 / 3)
                {
                    this.Colonist.IsWorking = false;
                    if (this.Colonist.RaisedArmsFrame == 0) this.IsFinished = true;
                }
            }
            else if (counter >= 60)
            {
                // Finished
                if (!MicrobotParticleController.ActiveColonists.Contains(this.Colonist.Id))
                {
                    this.Colonist.IsWorking = false;
                    if (this.Colonist.RaisedArmsFrame == 0)
                    {
                        this.IsFinished = true;
                    }
                }
            }
            else if (counter == 30)
            {
                // Transfer resource
                if (this.Target is IResourceStack rs)
                {
                    if (rs.ItemCount < rs.MaxItems && this.Colonist.CarriedItemTypeBack == rs.ItemType)
                    {
                        rs.AddItem();

                        if (rs.RenderAlpha < 1)
                        {
                            // Newly created stack, set alpha to one and remove the blueprint
                            rs.RenderAlpha = 1f;
                            rs.ShadowAlpha = 1f;
                            foreach (var blueprint in World.ConfirmedBlueprints.Where(b => b.Value.MainTile == rs.MainTile && b.Value.ThingType == rs.ThingType))
                            {
                                World.ConfirmedBlueprints.Remove(blueprint.Key);
                                EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Removed, blueprint.Value);
                                break;
                            }
                        }

                        this.Colonist.CarriedItemTypeBack = ItemType.None;
                        EventManager.EnqueueWorldPropertyChangeEvent(this.Colonist.Id, nameof(IColonist.CarriedItemTypeBack), this.Colonist.MainTile.Row, ThingType.Colonist);
                    }
                }
            }
            else
            {
                if (this.Target is IResourceStack && this.Colonist.RaisedArmsFrame == 18)
                {
                    // Particle effect for stack dropoff
                    this.DoParticles();
                }
            }

            base.Update();
        }

        private void DoParticles()
        {
            var renderTile = this.Colonist.MainTile;
            var toolOffset = this.GetToolOffset() + this.Colonist.PositionOffset;

            // Render in a different tile if facing N, to fix render order problem
            if (this.Colonist.FacingDirection.In(Direction.NW, Direction.N, Direction.NE))
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
                var tile = this.Target.MainTile;
                var offsetX = ((Rand.NextFloat() - 0.5f) * 10f) + tile.CentrePosition.X - renderTile.CentrePosition.X;
                var offsetY = ((Rand.NextFloat() - 0.5f) * 5f) + tile.CentrePosition.Y - renderTile.CentrePosition.Y;

                MicrobotParticleController.AddParticle(renderTile, toolOffset.X, toolOffset.Y, z, offsetX, offsetY, 0f, this.Colonist.Id, false);
            }
        }
    }
}
