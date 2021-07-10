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

    [ProtoContract]
    public class ActivityGeology : ActivityBase
    {
        private const float tileOffset = 0.3f;

        [ProtoMember(1)]
        private Direction approachingFromDirection;

        [ProtoMember(2)]
        private readonly int? targetTileIndex;

        // Deserialisation ctor
        protected ActivityGeology() {}

        public ActivityGeology(IColonist colonist, Path path = null) : base(colonist)
        {
            this.targetTileIndex = path == null ? colonist.MainTileIndex : World.GetSmallTile(path.EndPosition)?.Index;
            UpdateCurrentAction(colonist, path);
            if (this.CurrentAction?.IsFinished != false) this.IsFinished = true;
        }

        private void UpdateCurrentAction(IColonist colonist, Path path = null)
        {
            if (this.Colonist.WorkPriorities[ColonistPriority.Geology] == 0)
            {
                this.IsFinished = true;
                return;
            }

            if (path == null && this.targetTileIndex.HasValue && this.targetTileIndex != this.Colonist.MainTileIndex)
            {
                // Can happen if path blocked or something changed - we are not where we intended
                this.IsFinished = true;
                return;
            }

            var endTile = path == null ? colonist.MainTile : World.GetSmallTile(path.EndPosition);
            if (endTile?.IsMineResourceVisible != false)
            {
                this.IsFinished = true;
                return;
            }

            // If we were given a path, then start moving.
            if (path != null)
            {
                var nodes = path.RemainingNodes.ToList();
                if (nodes.Count > 1)
                {
                    var lastButOneNode = nodes[nodes.Count - 2];
                    var lastButOneTile = World.GetSmallTile(lastButOneNode.X, lastButOneNode.Y);
                    this.approachingFromDirection = DirectionHelper.GetDirectionFromAdjacentPositions(endTile.X, endTile.Y, lastButOneTile.X, lastButOneTile.Y);
                    if (this.approachingFromDirection == Direction.None || this.approachingFromDirection == Direction.S) this.approachingFromDirection = Direction.SW;
                    else if (this.approachingFromDirection == Direction.E) this.approachingFromDirection = Direction.SE;
                    else if (this.approachingFromDirection == Direction.N) this.approachingFromDirection = Direction.NE;
                    else if (this.approachingFromDirection == Direction.W) this.approachingFromDirection = Direction.NW;
                    lastButOneTile = endTile.GetTileToDirection(this.approachingFromDirection);
                    var positionOffset = new Vector2f(-tileOffset * (endTile.TerrainPosition.X - lastButOneTile.TerrainPosition.X), -tileOffset * (endTile.TerrainPosition.Y - lastButOneTile.TerrainPosition.Y));
                    this.CurrentAction = new ActionWalk(colonist, path, positionOffset, DirectionHelper.Reverse(this.approachingFromDirection));
                }
                else
                {
                    this.approachingFromDirection = Direction.SW;
                    var lastButOneTile = endTile.TileToSW;
                    if (endTile != null)
                    {
                        var positionOffset = new Vector2f(-tileOffset * (endTile.TerrainPosition.X - lastButOneTile.TerrainPosition.X), -tileOffset * (endTile.TerrainPosition.Y - lastButOneTile.TerrainPosition.Y));
                        this.CurrentAction = new ActionWalk(colonist, path, positionOffset, DirectionHelper.Reverse(this.approachingFromDirection));
                    }
                    else this.CurrentAction = new ActionWalk(colonist, path, new Vector2f());
                }
            }
            else
            {
                if (this.approachingFromDirection == Direction.None) this.approachingFromDirection = Direction.SW;
                var approachingFromTile = endTile.GetTileToDirection(this.approachingFromDirection);
                if (approachingFromTile != null)
                {
                    var position = new Vector2f(this.Colonist.MainTile.X - (tileOffset * (this.Colonist.MainTile.X - approachingFromTile.TerrainPosition.X)), this.Colonist.MainTile.Y - (tileOffset * (this.Colonist.MainTile.Y - approachingFromTile.TerrainPosition.Y)));
                    if (this.Colonist.Rotation.ApproxEquals(DirectionHelper.GetAngleFromDirection(DirectionHelper.Reverse(this.approachingFromDirection)), 0.001f) && (this.Colonist.Position - position).Length() < 0.1f)
                    {
                        // In place and facing the right way.
                        this.CurrentAction = new ActionGeology(colonist);
                    }
                    else
                    {
                        // Make an empty path so that we can move to the required rotation and offset
                        this.BuildWalkActionForFacingTarget(DirectionHelper.Reverse(this.approachingFromDirection), position - this.Colonist.Position);
                    }
                }
            }
        }

        public override void Update()
        {
            // Geology deselected?
            if (this.Colonist.WorkPriorities[ColonistPriority.Geology] == 0)
            {
                if (this.CurrentAction is ActionWalk w) w.Path.RemainingNodes.Clear();
            }
            else
            {
                var walkAction = this.CurrentAction as ActionWalk;
                if (walkAction == null || walkAction.Path != null)
                {
                    var tile = walkAction != null ? World.GetSmallTile(walkAction.Path.EndPosition) : this.Colonist.MainTile;
                    try
                    {
                        tile.ReserveForResourceSurvey(this.colonistId);
                    }
                    catch
                    {
                        // Failed - null ref.  Clear tile survey flag.
                        if (GeologyController.TilesToSurvey.Contains(tile.Index)) GeologyController.Toggle(tile);
                        this.IsFinished = true;
                        return;
                    }
                }
            }

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
