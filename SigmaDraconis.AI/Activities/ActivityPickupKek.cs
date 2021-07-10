namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivityPickupKek : ActivityBase
    {
        [ProtoMember(1)]
        public int TargetId { get; private set; }

        // Deserialisation ctor
        protected ActivityPickupKek() { }

        public ActivityPickupKek(IColonist colonist, IColonistInteractive target, Path path = null) : base(colonist, target.Id)
        {
            this.TargetId = target.Id;
            UpdateCurrentAction(colonist, path);
            if (this.CurrentAction?.IsFinished != false) this.IsFinished = true;
        }

        private void UpdateCurrentAction(IColonist colonist, Path path = null)
        {
            if (colonist.CarriedItemTypeArms != ItemType.None)
            {
                return;
            }

            // If we were given a path, then start moving.  If not then we are already next to a dispenser.
            if (path != null)
            {
                // Find the dispenser that is next to the end tile, ideally ready to use
                var endTile = World.GetSmallTile(path.EndPosition);
                if (!(World.GetThing(this.TargetId) is IKekDispenser dispenser))
                {
                    this.IsFinished = true;
                    return;
                }

                dispenser.AssignColonist(this.Colonist.Id, endTile.Index);
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(endTile.X, endTile.Y, dispenser.MainTile.X, dispenser.MainTile.Y);
                var positionOffset = new Vector2f(0.4f * (dispenser.MainTile.TerrainPosition.X - endTile.TerrainPosition.X), 0.4f * (dispenser.MainTile.TerrainPosition.Y - endTile.TerrainPosition.Y));
                this.CurrentAction = new ActionWalk(colonist, path, positionOffset, direction);
            }
            else
            {
                // Should be next to the dispenser
                if (!(World.GetThing(this.TargetId) is IKekDispenser dispenser) || dispenser.DispenserStatus == DispenserStatus.NoResource || !dispenser.CanAssignColonist(this.colonistId, this.Colonist.MainTileIndex))
                {
                    this.IsFinished = true;
                    return;
                }

                dispenser.AssignColonist(this.Colonist.Id, this.Colonist.MainTile.Index);
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(this.Colonist.MainTile.X, this.Colonist.MainTile.Y, dispenser.MainTile.X, dispenser.MainTile.Y);
                var position = new Vector2f(this.Colonist.MainTile.X + (0.4f * (dispenser.MainTile.X - this.Colonist.MainTile.X)), this.Colonist.MainTile.Y + (0.4f * (dispenser.MainTile.Y - this.Colonist.MainTile.Y)));
                if (this.Colonist.Rotation.ApproxEquals(DirectionHelper.GetAngleFromDirection(direction), 0.001f)
                    && (this.Colonist.Position - position).Length() < 0.1f)
                {
                    this.CurrentAction = new ActionPickupKek(colonist, dispenser.Id);
                }
                else
                {
                    // Make an empty path so that we can move to the required rotation and offset
                    this.BuildWalkActionForFacingTarget(direction, new Vector2f(0.4f * (dispenser.MainTile.X - this.Colonist.MainTile.X), 0.4f * (dispenser.MainTile.Y - this.Colonist.MainTile.Y)));
                }
            }
        }

        public override void Update()
        {
            if (this.CurrentAction?.IsFinished != false)
            {
                if (this.CurrentAction.IsFailed)
                {
                    this.IsFinished = true;
                    return;
                }

                this.UpdateCurrentAction(this.Colonist);
                if (this.CurrentAction?.IsFinished != false)
                {
                    this.IsFinished = true;
                    return;
                }
            }

            this.CurrentAction.Update();
        }
    }
}
