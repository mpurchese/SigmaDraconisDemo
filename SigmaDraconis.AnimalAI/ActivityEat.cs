namespace SigmaDraconis.AnimalAI
{
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivityEat : ActivityBase
    {
        [ProtoMember(1)]
        private readonly int frames;

        [ProtoMember(2)]
        private readonly int targetID;

        // Deserialisation ctor
        protected ActivityEat() { }

        public ActivityEat(IAnimal animal, int frames, ISmallTile targetTile, int targetID, Path path = null) : base(animal)
        {
            this.frames = frames;
            this.targetID = targetID;

            var plant = World.GetThing(targetID) as IFruitPlant;
            if (plant == null || plant.CountFruitAvailable == 0 || plant.HarvestJobProgress.GetValueOrDefault() > 0)
            {
                this.IsFinished = true;
                return;
            }

            var p = plant as IPositionOffsettable;
            var plantOffetX = p != null ? p.PositionOffset.X : 0;
            var plantOffetY = p != null ? p.PositionOffset.Y : 0;

            if (path != null)
            {
                var endTile = World.GetSmallTile(path.EndPosition);
                var o = plant.Definition.TileBlockModel == TileBlockModel.Point ? 0.6f : 0.5f;
                var positionOffset = new Vector2f(plantOffetX + (o * (targetTile.X - endTile.X)), plantOffetY + (o * (targetTile.Y - endTile.Y)));
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(endTile.X, endTile.Y, targetTile.X, targetTile.Y);

                this.CurrentAction = new ActionWalk(animal, path, positionOffset, direction);
            }
            else
            {
                var o = plant.Definition.TileBlockModel == TileBlockModel.Point ? 0.6f : 0.5f;
                var positionOffset = new Vector2f(plantOffetX + (o * (targetTile.X - this.Animal.MainTile.X)), plantOffetY + (o * (targetTile.Y - this.Animal.MainTile.Y)));
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(this.Animal.MainTile.X, this.Animal.MainTile.Y, targetTile.X, targetTile.Y);
                var position = new Vector2f(this.Animal.MainTile.X + positionOffset.X, this.Animal.MainTile.Y + positionOffset.Y);

                if (this.Animal.Rotation.ApproxEquals(DirectionHelper.GetAngleFromDirection(direction), 0.001f) && (this.Animal.Position - position).Length() < 0.1f)
                {
                    // In place and facing the right way.
                    this.CurrentAction = new ActionEat(this.Animal, targetID, frames);
                }
                else
                {
                    // Make an empty path so that we can move to the required rotation and offset
                    this.BuildWalkActionForFacingTarget(direction, positionOffset);
                }
            }
        }

        public override void Update()
        {
            if (this.CurrentAction?.IsFinished != true) this.CurrentAction.Update();
            if (this.CurrentAction?.IsFinished != false)
            {
                if (this.CurrentAction is ActionWalk) this.CurrentAction = new ActionEat(this.Animal, this.targetID, this.frames);
                else if (this.CurrentAction is ActionEat) this.CurrentAction = new ActionWait(this.Animal, 60);   // Stop for a moment after eating
                else this.IsFinished = true;
            }
        }
    }
}
