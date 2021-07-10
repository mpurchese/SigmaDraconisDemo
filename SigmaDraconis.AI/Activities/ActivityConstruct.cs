namespace SigmaDraconis.AI
{
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Config;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivityConstruct : ActivityBase
    {
        [ProtoMember(1)]
        public int TargetID { get; set; }

        // Deserialisation ctor
        protected ActivityConstruct() { }

        public ActivityConstruct(IColonist colonist, IBlueprint target, Path path = null) : base(colonist, target.Id)
        {
            this.TargetID = target.Id;
            UpdateCurrentAction(colonist, path);
            if (this.CurrentAction?.IsFinished != false) this.IsFinished = true;
        }

        private void UpdateCurrentAction(IColonist colonist, Path path = null)
        {
            if (this.Colonist.WorkPriorities[ColonistPriority.Construct] == 0)
            {
                this.IsFinished = true;
                return;
            }

            // If we were given a path, then start moving.  If not then we are already next to a target.
            if (path != null)
            {
                // Find the blueprint that is next to the end tile
                var endTile = World.GetSmallTile(path.EndPosition);
                var target = World.ConfirmedBlueprints.Values.OfType<IColonistInteractive>()
                    .Where(b => CanColonistBuildBlueprint(b as IBlueprint))
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
                var targets = World.ConfirmedBlueprints.Values.OfType<IColonistInteractive>()
                    // Conduit must connect to another conduit but not be an extension to an existing conduit
                    .Where(b => CanColonistBuildBlueprint(b as IBlueprint))
                    .Where(d => d != null && d.GetAccessTiles(this.Colonist.Id).Contains(this.Colonist.MainTile)).ToList();

                if (!targets.Any())
                {
                    this.IsFinished = true;
                    return;
                }

                // Build in order - conduits then foundations then other then roofs
                var target = targets.FirstOrDefault(t => t.ThingType.IsFoundation());
                if (target == null) target = targets.FirstOrDefault(t => t.ThingType != ThingType.Roof);
                if (target == null) target = targets.FirstOrDefault();

                if (target == null || !(target is IBlueprint blueprint))
                {                
                    this.IsFinished = true;
                    return;
                }

                target.AssignColonist(this.Colonist.Id, this.Colonist.MainTile.Index);

                var allTiles = new List<ISmallTile>();
                if (blueprint.ThingType.In(ThingType.Wall, ThingType.Door))
                {
                    // Walls and doors have two tiles for our purposes, but the blueprint only has one.
                    allTiles.Add(target.MainTile);
                    var tile2 = target.MainTile.GetTileToDirection(blueprint.Direction);
                    if (tile2 != null) allTiles.Add(tile2);
                }
                else
                {
                    allTiles = target.AllTiles.ToList();
                }

                var direction = this.Colonist.FacingDirection;
                if (blueprint.ThingType != ThingType.Roof || !allTiles.Contains(this.Colonist.MainTile.GetTileToDirection(direction)))
                {
                    for (int i = 7; i >= 0; i--)
                    {
                        direction = (Direction)i;
                        if (allTiles.Contains(this.Colonist.MainTile.GetTileToDirection(direction))) break;
                    }
                }

                if (target.ThingType == ThingType.Roof)
                {
                    this.CurrentAction = new ActionConstructRoof(colonist, target as IBlueprint);
                }
                else if (this.Colonist.Rotation.ApproxEquals(DirectionHelper.GetAngleFromDirection(direction), 0.001f))
                {
                    // In place and facing the right way.
                    this.CurrentAction = new ActionConstruct(colonist, target as IBlueprint);
                }
                else
                {
                    // Make an empty path so that we can move to the required rotation and offset
                    this.BuildWalkActionForFacingTarget(direction);
                }
            }
        }

        public static bool CanColonistBuildBlueprint(IBlueprint blueprint)
        {
            if (blueprint.ThingType == ThingType.ConduitMinor || blueprint.ThingType == ThingType.ConduitMajor || Constants.ItemTypesByResourceStackType.ContainsKey(blueprint.ThingType)) return false;   // Resource stacks and conduits not created in this way
            return true;
        }

        public override void Update()
        {
            // Work type deselected?
            if (this.Colonist.WorkPriorities[ColonistPriority.Construct] == 0 && this.CurrentAction is ActionWalk w) w.Path.RemainingNodes.Clear();

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
