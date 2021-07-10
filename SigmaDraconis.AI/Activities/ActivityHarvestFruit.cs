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
    public class ActivityHarvestFruit : ActivityBase
    {
        [ProtoMember(1)]
        public int PlantId { get; private set; }

        // Deserialisation ctor
        protected ActivityHarvestFruit() { }

        public ActivityHarvestFruit(IColonist colonist, IFruitPlant plant, Path path = null) : base(colonist, plant.Id)
        {
            this.PlantId = plant.Id;
            UpdateCurrentAction(colonist, path);
            if (this.CurrentAction?.IsFinished != false) this.IsFinished = true;
        }

        private void UpdateCurrentAction(IColonist colonist, Path path = null)
        {
            var isCookerAvailable = World.GetThings<ICooker>(ThingType.Cooker).Any(t => t.IsReadyToCook);

            if (this.Colonist.WorkPriorities[ColonistPriority.FarmHarvest] == 0 || this.Colonist.CarriedItemTypeBack != ItemType.None
                || !(World.GetThing(this.PlantId) is IFruitPlant plant) || plant.CountFruitAvailable == 0 || plant.HarvestFruitPriority == WorkPriority.Disabled)
            {
                this.IsFinished = true;
                return;
            }

            // If we were given a path, then start moving.  If not then we are already next to a fruit plant.
            if (path != null)
            {
                // Find the planter that is next to the end tile
                var endTile = World.GetSmallTile(path.EndPosition);
                var plantTile = plant.AllTiles.FirstOrDefault(t => endTile.AdjacentTiles8.Contains(t));
                if (!plant.CanAssignColonist(this.colonistId, endTile.Index) || plantTile == null)
                {
                    this.IsFinished = true;
                    return;
                }

                plant.AssignColonist(this.Colonist.Id, endTile.Index);
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(endTile.X, endTile.Y, plantTile.X, plantTile.Y);
                var positionOffset = new Vector2f(0.3f * (plantTile.TerrainPosition.X - endTile.TerrainPosition.X), 0.3f * (plantTile.TerrainPosition.Y - endTile.TerrainPosition.Y));
                this.CurrentAction = new ActionWalk(colonist, path, positionOffset, direction, 0.25f);
            }
            else
            {
                // Should be next to a planter.  If more then one then pick the one that is ready.
                var plantTile = plant.AllTiles.FirstOrDefault(t => this.Colonist.MainTile.AdjacentTiles8.Contains(t));
                if (!plant.CanAssignColonist(this.colonistId, this.Colonist.MainTileIndex) || plantTile == null)
                {
                    this.IsFinished = true;
                    return;
                }

                plant.AssignColonist(this.Colonist.Id, this.Colonist.MainTile.Index);
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(this.Colonist.MainTile.X, this.Colonist.MainTile.Y, plantTile.X, plantTile.Y);
                var position = new Vector2f(this.Colonist.MainTile.X + (0.3f * (plantTile.X - this.Colonist.MainTile.X)), this.Colonist.MainTile.Y + (0.3f * (plantTile.Y - this.Colonist.MainTile.Y)));
                if (this.Colonist.Rotation.ApproxEquals(DirectionHelper.GetAngleFromDirection(direction), 0.001f)
                    && (this.Colonist.Position - position).Length() <= 0.25f)
                {
                    // In place and facing the right way.
                    this.CurrentAction = new ActionHarvestFruit(colonist, plant);
                }
                else
                {
                    // Make an empty path so that we can move to the required rotation and offset
                    this.BuildWalkActionForFacingTarget(direction, new Vector2f(0.3f * (plantTile.X - this.Colonist.MainTile.X), 0.3f * (plantTile.Y - this.Colonist.MainTile.Y)), 0.25f);
                }
            }
        }

        public override void Update()
        {
            // Farming deselected?
            if (this.Colonist.WorkPriorities[ColonistPriority.FarmHarvest] == 0 && this.CurrentAction is ActionWalk w) w.Path.RemainingNodes.Clear();

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
