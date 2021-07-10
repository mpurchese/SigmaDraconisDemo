namespace SigmaDraconis.AI
{
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivityDeconstruct : ActivityBase
    {
        [ProtoMember(1)]
        public int TargetID { get; set; }

        // Deserialisation ctor
        protected ActivityDeconstruct() { }

        public ActivityDeconstruct(IColonist colonist, IColonistInteractive target, Path path = null) : base(colonist, target.Id)
        {
            this.TargetID = target.Id;
            UpdateCurrentAction(colonist, path);
            if (this.CurrentAction?.IsFinished != false) this.IsFinished = true;
        }

        private void UpdateCurrentAction(IColonist colonist, Path path = null)
        {
            if (this.Colonist.WorkPriorities[ColonistPriority.Deconstruct] == 0)
            {
                this.IsFinished = true;
                return;
            }

            // If we were given a path, then start moving.  If not then we are already next to a target.
            if (path != null)
            {
                // Find the stack that is next to the end tile
                var endTile = World.GetSmallTile(path.EndPosition);
                var target = World.ResourcesForDeconstruction.Keys.Select(r => World.GetThing(r) as IColonistInteractive)
                    .Where(d => d != null && d.GetAccessTiles(this.Colonist.Id).Contains(endTile))
                    .FirstOrDefault();
                if (target == null)
                {
                    this.IsFinished = true;
                    return;
                }

                target.AssignColonist(this.Colonist.Id, endTile.Index);
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(endTile.X, endTile.Y, target.MainTile.X, target.MainTile.Y);

                Vector2f endOffset = FindPositionOffsetAvoidingPointBlocker(endTile);
                this.CurrentAction = new ActionWalk(colonist, path, endOffset, direction, 0.25f);
            }
            else
            {
                // Should be next to a target.
                var target = World.ResourcesForDeconstruction.Keys.Select(r => World.GetThing(r) as IColonistInteractive)
                    .Where(d => d != null && d.GetAccessTiles(this.Colonist.Id).Contains(this.Colonist.MainTile))
                    .FirstOrDefault();
                if (target == null)
                {
                    this.IsFinished = true;
                    return;
                }

                target.AssignColonist(this.Colonist.Id, this.Colonist.MainTile.Index);

                var direction = this.Colonist.FacingDirection;
                if (!this.Colonist.MainTile.GetTileToDirection(direction).ThingsAll.Contains(target))
                {
                    for (int i = 7; i >= 0; i--)
                    {
                        direction = (Direction)i;
                        if (this.Colonist.MainTile.GetTileToDirection(direction).ThingsAll.Contains(target)) break;
                    }
                }

                if (this.Colonist.Rotation.ApproxEquals(DirectionHelper.GetAngleFromDirection(direction), 0.001f))
                {
                    // In place and facing the right way.
                    this.CurrentAction = new ActionDeconstruct(colonist, target as IRecyclableThing);
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
            if (this.Colonist.WorkPriorities[ColonistPriority.Deconstruct] == 0 && this.CurrentAction is ActionWalk w) w.Path.RemainingNodes.Clear();

            if (this.CurrentAction?.IsFinished != false)
            {
                if (this.CurrentAction?.IsFailed != false || (this.CurrentAction is ActionDeconstruct && this.CurrentAction.IsFinished))
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
