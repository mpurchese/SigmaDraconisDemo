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
    /// Haul something we are carrying to a resource processor
    /// </summary>
    [ProtoContract]
    public class ActivityHaulToNetwork : ActivityBase
    {
        [ProtoMember(1)]
        public int TargetID { get; set; }

        private IColonistInteractive target;

        // Deserialisation ctor
        protected ActivityHaulToNetwork() { }

        public ActivityHaulToNetwork(IColonist colonist, IColonistInteractive target, Path path = null) : base(colonist, target.Id, colonist.CarriedItemTypeBack)
        {
            this.TargetID = target.Id;
            this.target = target;
            UpdateCurrentAction(colonist, path);
            if (this.CurrentAction?.IsFinished != false)
            {
                this.IsFinished = true;
                ResourceStackingController.JobCompleted(this.Colonist);
            }
        }

        public override void AfterDeserialization()
        {
            this.target = World.GetThing(this.TargetID) as IColonistInteractive;
            base.AfterDeserialization();
        }

        private void UpdateCurrentAction(IColonist colonist, Path path = null)
        {
            // Sanity check
            if (this.target == null || colonist.CarriedItemTypeBack == ItemType.None || World.ResourceNetwork?.CanAddItem(colonist.CarriedItemTypeBack) != true
                || ResourceStackingController.GetJobForColonist(this.Colonist)?.Target != this.target)
            {
                this.IsFinished = true;
                return;
            }

            // If we were given a path, then start moving.  If not then we are already next to a resource stack.
            if (path != null)
            {
                var endTile = World.GetSmallTile(path.EndPosition);
                this.target.AssignColonist(this.Colonist.Id, endTile.Index);

                // End offset for dropoffs
                var positionOffset = new Vector2f(0.2f * (this.target.MainTile.TerrainPosition.X - endTile.TerrainPosition.X), 0.2f * (this.target.MainTile.TerrainPosition.Y - endTile.TerrainPosition.Y));

                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(endTile.X, endTile.Y, this.target.MainTile.X, this.target.MainTile.Y);
                this.CurrentAction = new ActionWalk(colonist, path, positionOffset, direction, 0.25f);
            }
            else
            {
                // Should be next to target.
                if (!this.target.GetAccessTiles(this.colonistId).Any(t => t.Index == this.Colonist.MainTileIndex))
                {
                    this.IsFinished = true;
                    return;
                }

                this.target.AssignColonist(this.Colonist.Id, this.Colonist.MainTile.Index);
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(this.Colonist.MainTile.X, this.Colonist.MainTile.Y, this.target.MainTile.X, this.target.MainTile.Y);
                if (this.Colonist.Rotation.ApproxEquals(DirectionHelper.GetAngleFromDirection(direction), 0.001f))
                {
                    // In place and facing the right way.
                    this.CurrentAction = new ActionDropoff(colonist, this.target);
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
                if (this.CurrentAction?.IsFailed != false || (this.CurrentAction is ActionDropoff && this.CurrentAction.IsFinished))
                {
                    ResourceStackingController.JobCompleted(this.Colonist);
                    this.IsFinished = true;
                    return;
                }

                this.UpdateCurrentAction(this.Colonist);
                if (this.CurrentAction?.IsFinished != false)
                {
                    ResourceStackingController.JobCompleted(this.Colonist);
                    this.IsFinished = true;
                    return;
                }
            }

            this.CurrentAction.Update();
        }
    }
}
