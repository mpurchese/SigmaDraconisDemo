namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivityRepair : ActivityBase
    {
        [ProtoMember(1)]
        public int TargetID { get; set; }

        // Deserialisation ctor
        protected ActivityRepair() { }

        public ActivityRepair(IColonist colonist, IRepairableThing target, Path path = null) : base(colonist, target.Id)
        {
            this.TargetID = target.Id;
            UpdateCurrentAction(colonist, path);
            if (this.CurrentAction?.IsFinished != false) this.IsFinished = true;
        }

        public override void AfterDeserialization()
        {
            // Sanity check
            var t = World.GetThing(this.TargetID) as IRepairableThing;
            if (t == null || !t.CanAssignColonistForRepair(colonistId)) this.IsFinished = true;

            base.AfterDeserialization();
            this.Colonist.TargetBuilingID = t.Id;
        }

        private void UpdateCurrentAction(IColonist colonist, Path path = null)
        {
            if (this.Colonist.WorkPriorities[ColonistPriority.Construct] == 0)
            {
                this.IsFinished = true;
                return;
            }

            if (!(World.GetThing(this.TargetID) is IRepairableThing target))
            {
                this.IsFinished = true;
                return;
            }

            // If we were given a path, then start moving.  If not then we are already next to a target.
            if (path != null)
            {
                // Find the blueprint that is next to the end tile
                var endTile = World.GetSmallTile(path.EndPosition);

                target.AssignColonistForRepair(this.Colonist.Id, endTile.Index);
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(endTile.X, endTile.Y, target.MainTile.X, target.MainTile.Y);

                Vector2f endOffset = FindPositionOffsetAvoidingPointBlocker(endTile);
                this.CurrentAction = new ActionWalk(colonist, path, endOffset, direction, 0.25f);
            }
            else if (!target.CanAssignColonistForRepair(this.Colonist.Id, this.Colonist.MainTileIndex))
            {
                // Walk action failed, we're not at target
                this.IsFinished = true;
                return;
            }
            else
            {
                target.AssignColonistForRepair(this.Colonist.Id, this.Colonist.MainTile.Index);

                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(this.Colonist.MainTile.X, this.Colonist.MainTile.Y, target.MainTile.X, target.MainTile.Y);
                if (this.Colonist.Rotation.ApproxEquals(DirectionHelper.GetAngleFromDirection(direction), 0.001f))
                {
                    // In place and facing the right way.
                    this.CurrentAction = new ActionRepair(colonist, target);
                }
                else
                {
                    // Make an empty path so that we can move to the required rotation and offset
                    this.BuildWalkActionForFacingTarget(direction);
                }
            }
        }

        public override void Update()
        {
            // Work type deselected?
            if (this.Colonist.WorkPriorities[ColonistPriority.Maintain] == 0 && this.CurrentAction is ActionWalk w) w.Path.RemainingNodes.Clear();

            if (this.CurrentAction?.IsFinished != false)
            {
                if (this.CurrentAction?.IsFailed != false || (!(this.CurrentAction is ActionWalk) && this.CurrentAction.IsFinished))
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
