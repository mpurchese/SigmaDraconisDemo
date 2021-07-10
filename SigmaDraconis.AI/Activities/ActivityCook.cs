namespace SigmaDraconis.AI
{
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;
    using Config;
    using Shared;
    using World;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivityCook : ActivityBase
    {
        [ProtoMember(1)]
        public int CookerId { get; private set; }

        // Deserialisation ctor
        protected ActivityCook() { }

        public ActivityCook(IColonist colonist, IColonistInteractive cooker, Path path = null) : base(colonist, cooker.Id)
        {
            this.CookerId = cooker.Id;
            UpdateCurrentAction(colonist, path);
            if (this.CurrentAction?.IsFinished != false) this.IsFinished = true;
        }

        private void UpdateCurrentAction(IColonist colonist, Path path = null)
        {
            if (!this.Colonist.CarriedCropType.HasValue)
            {
                this.IsFinished = true;
                return;
            }

            var thingType = CropDefinitionManager.GetDefinition(this.Colonist.CarriedCropType.Value)?.CookerType ?? ThingType.Cooker;

            // If we were given a path, then start moving.  If not then we are already next to a cooker.
            if (path != null)
            {
                // Find the planter that is next to the end tile
                var endTile = World.GetSmallTile(path.EndPosition);
                var cooker = World.GetThings<ICooker>(thingType)
                    .Where(d => d.GetAccessTiles(this.Colonist.Id).Contains(endTile))
                    .OrderBy(d => d.FactoryStatus == FactoryStatus.Standby ? 1 : 2)
                    .FirstOrDefault();
                if (cooker == null)
                {
                    this.IsFinished = true;
                    return;
                }

                cooker.AssignColonist(this.Colonist.Id, endTile.Index);
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(endTile.X, endTile.Y, cooker.MainTile.X, cooker.MainTile.Y);
                var positionOffset = new Vector2f(0.3f * (cooker.MainTile.TerrainPosition.X - endTile.TerrainPosition.X), 0.3f * (cooker.MainTile.TerrainPosition.Y - endTile.TerrainPosition.Y));
                this.CurrentAction = new ActionWalk(colonist, path, positionOffset, direction);
            }
            else
            {
                // Should be next to a cooker.  If more then one then pick the one that is ready.
                var cooker = World.GetThings<ICooker>(thingType)
                    .Where(d => d.GetAccessTiles(this.Colonist.Id).Contains(this.Colonist.MainTile))
                    .OrderBy(d => d.FactoryStatus == FactoryStatus.Standby ? 1 : 2)
                    .FirstOrDefault();
                if (cooker == null)
                {
                    this.IsFinished = true;
                    return;
                }

                cooker.AssignColonist(this.Colonist.Id, this.Colonist.MainTile.Index);
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(this.Colonist.MainTile.X, this.Colonist.MainTile.Y, cooker.MainTile.X, cooker.MainTile.Y);
                var position = new Vector2f(this.Colonist.MainTile.X + (0.3f * (cooker.MainTile.X - this.Colonist.MainTile.X)), this.Colonist.MainTile.Y + (0.3f * (cooker.MainTile.Y - this.Colonist.MainTile.Y)));
                if (this.Colonist.Rotation.ApproxEquals(DirectionHelper.GetAngleFromDirection(direction), 0.001f)
                    && (this.Colonist.Position - position).Length() < 0.1f)
                {
                    // In place and facing the right way.
                    if (cooker.FactoryStatus != FactoryStatus.Standby)
                    {
                        this.CurrentAction = new ActionWait(this.Colonist);
                    }
                    else
                    {
                        this.CurrentAction = new ActionCook(colonist, cooker);
                    }
                }
                else
                {
                    // Make an empty path so that we can move to the required rotation and offset
                    this.BuildWalkActionForFacingTarget(direction, new Vector2f(0.3f * (cooker.MainTile.X - this.Colonist.MainTile.X), 0.3f * (cooker.MainTile.Y - this.Colonist.MainTile.Y)));
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
