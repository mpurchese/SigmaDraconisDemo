namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivityRelax : ActivityBase
    {
        [ProtoMember(1)]
        public int TableId { get; private set; }

        // Deserialisation ctor
        protected ActivityRelax() { }

        public ActivityRelax(IColonist colonist, int tableId, Path path = null) : base(colonist, tableId)
        {
            this.TableId = tableId;
            UpdateCurrentAction(colonist, path);
            if (this.CurrentAction?.IsFinished != false) this.IsFinished = true;
        }

        private void UpdateCurrentAction(IColonist colonist, Path path = null)
        {
            if (!(World.GetThing(this.TableId) is ITable table))
            {
                this.IsFinished = true;
                return;
            }

            // If we were given a path, then start moving.  If not then we are already next to a table.
            if (path != null)
            {
                var endTile = World.GetSmallTile(path.EndPosition);
                table.AssignColonist(this.Colonist.Id, endTile.Index);
                var positionOffset = new Vector2f(0.48f * (table.MainTile.TerrainPosition.X - endTile.TerrainPosition.X), 0.48f * (table.MainTile.TerrainPosition.Y - endTile.TerrainPosition.Y));
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(endTile.X, endTile.Y, table.MainTile.X, table.MainTile.Y);
                this.CurrentAction = new ActionWalk(colonist, path, positionOffset, direction);
            }
            else if (table.CanAssignColonist(this.Colonist.Id, this.Colonist.MainTile.Index))
            {
                table.AssignColonist(this.Colonist.Id, this.Colonist.MainTile.Index);
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(this.Colonist.MainTile.X, this.Colonist.MainTile.Y, table.MainTile.X, table.MainTile.Y);
                var position = new Vector2f((this.Colonist.MainTile.X + (0.92f * table.MainTile.X)) / 1.92f, (this.Colonist.MainTile.Y + (0.92f * table.MainTile.Y)) / 1.92f);
                if (this.Colonist.Rotation.ApproxEquals(DirectionHelper.GetAngleFromDirection(direction), 0.001f)
                    && (this.Colonist.Position - position).Length() < 0.1f)
                {
                    this.CurrentAction = new ActionRelax(colonist, table);
                }
                else
                {
                    // Make an empty path so that we can move to the required rotation and offset
                    this.BuildWalkActionForFacingTarget(direction, new Vector2f(0.48f * (table.MainTile.X - this.Colonist.MainTile.X), 0.48f * (table.MainTile.Y - this.Colonist.MainTile.Y)));
                }
            }
            else this.IsFinished = true;
        }

        public override void Update()
        {
            if (this.CurrentAction?.IsFinished != false)
            {
                if (this.CurrentAction is ActionRelax || this.CurrentAction.IsFailed)
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
