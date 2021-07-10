namespace SigmaDraconis.AI
{
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.PathFinding;
    using WorldControllers;
    using WorldInterfaces;

    /// <summary>
    /// Go to a resource processor to pick something up
    /// </summary>
    [ProtoContract]
    public class ActivityHaulFromNetwork : ActivityBase
    {
        [ProtoMember(1)]
        public int TargetID { get; set; }

        [ProtoMember(2)]
        public ItemType ItemType { get; set; }

        private IColonistInteractive target;

        // Deserialisation ctor
        protected ActivityHaulFromNetwork() { }

        public ActivityHaulFromNetwork(IColonist colonist, IColonistInteractive target, ItemType itemType, Path path = null) : base(colonist, target.Id, itemType)
        {
            this.ItemType = itemType;
            this.TargetID = target.Id;
            this.target = target;
            UpdateCurrentAction(colonist, path);
            if (this.CurrentAction?.IsFinished != false) this.IsFinished = true;
        }

        public override void AfterDeserialization()
        {
            this.target = World.GetThing(this.TargetID) as IColonistInteractive;
            base.AfterDeserialization();
        }

        private void UpdateCurrentAction(IColonist colonist, Path path = null)
        {
            // Sanity check
            var job = ResourceStackingController.GetJobForColonist(this.Colonist);
            if (this.target == null || colonist.CarriedItemTypeBack != ItemType.None || job?.Source != this.target)
            {
                this.IsFinished = true;
                return;
            }

            this.ItemType = job.ItemType;   // In case it changed - this can happen

            // If we were given a path, then start moving.  If not then we are already next to target.
            if (path != null)
            {
                var endTile = World.GetSmallTile(path.EndPosition);
                target.AssignColonist(this.Colonist.Id, endTile.Index);

                // End offset for dropoffs
                var positionOffset = new Vector2f(0.2f * (target.MainTile.TerrainPosition.X - endTile.TerrainPosition.X), 0.2f * (target.MainTile.TerrainPosition.Y - endTile.TerrainPosition.Y));

                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(endTile.X, endTile.Y, target.MainTile.X, target.MainTile.Y);
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
                    this.CurrentAction = new ActionPickupFromNetwork(colonist, this.target, this.ItemType);
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
                if (this.CurrentAction?.IsFailed != false || (this.CurrentAction is ActionPickupFromNetwork && this.CurrentAction.IsFinished))
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
