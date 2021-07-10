namespace SigmaDraconis.AI
{
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.PathFinding;
    using WorldInterfaces;

    /// <summary>
    /// Go to a resource stack to pick something up
    /// </summary>
    [ProtoContract]
    public class ActivityHaulFromStack : ActivityBase
    {
        [ProtoMember(1)]
        public int TargetID { get; set; }

        private IResourceStack target;

        // Deserialisation ctor
        protected ActivityHaulFromStack() { }

        public ActivityHaulFromStack(IColonist colonist, IResourceStack target, Path path = null) : base(colonist, target.Id, target.ItemType)
        {
            this.TargetID = target.Id;
            this.target = target;
            UpdateCurrentAction(colonist, path);
            if (this.CurrentAction?.IsFinished != false) this.IsFinished = true;
        }

        public override void AfterDeserialization()
        {
            this.target = World.GetThing(this.TargetID) as IResourceStack;
            base.AfterDeserialization();
        }

        private void UpdateCurrentAction(IColonist colonist, Path path = null)
        {
            if (ResourceStackingController.GetJobForColonist(this.Colonist)?.Source != this.target)
            {
                this.IsFinished = true;
                return;
            }

            // If we were given a path, then start moving.  If not then we are already next to a resource stack.
            if (path != null)
            {
                var endTile = World.GetSmallTile(path.EndPosition);
                this.target.AssignColonist(this.Colonist.Id, endTile.Index);

                // End offset for stacks
                Vector2f endOffset = FindPositionOffsetAvoidingPointBlocker(endTile);

                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(endTile.X, endTile.Y, this.target.MainTile.X, this.target.MainTile.Y);
                this.CurrentAction = new ActionWalk(colonist, path, endOffset, direction, 0.25f);
            }
            else
            {
                // Should be next to target.
                if (!this.target.GetAccessTiles(this.colonistId).Any(t => t.Index == this.Colonist.MainTileIndex))
                {
                    this.IsFinished = true;
                    return;
                }

                this.target.AssignColonist(this.Colonist.Id, this.Colonist.MainTileIndex);
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(this.Colonist.MainTile.X, this.Colonist.MainTile.Y, this.target.MainTile.X, this.target.MainTile.Y);
                if (this.Colonist.Rotation.ApproxEquals(DirectionHelper.GetAngleFromDirection(direction), 0.001f))
                {
                    // In place and facing the right way.
                    this.CurrentAction = new ActionPickupFromStack(colonist, this.target);
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
            if (this.CurrentAction?.IsFinished != false)
            {
                if (this.CurrentAction?.IsFailed != false || (this.CurrentAction is ActionPickupFromStack && this.CurrentAction.IsFinished))
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
