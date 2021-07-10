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
    public class ActivityFarmPlant : ActivityBase
    {
        [ProtoMember(1)]
        public int PlanterId { get; private set; }

        // Deserialisation ctor
        protected ActivityFarmPlant() { }

        public ActivityFarmPlant(IColonist colonist, IPlanter planter, Path path = null) : base(colonist, planter.Id)
        {
            this.PlanterId = planter.Id;
            UpdateCurrentAction(colonist, path);
            if (this.CurrentAction?.IsFinished != false) this.IsFinished = true;
        }

        private void UpdateCurrentAction(IColonist colonist, Path path = null)
        {
            if (this.Colonist.WorkPriorities[ColonistPriority.FarmPlant] == 0)
            {
                this.IsFinished = true;
                return;
            }

            // If we were given a path, then start moving.  If not then we are already next to a planter.
            if (path != null)
            {
                // Find the planter that is next to the end tile
                var endTile = World.GetSmallTile(path.EndPosition);
                var planter = World.GetPlanters().Where(d => this.CanWork(d, endTile)).FirstOrDefault();
                if (planter == null)
                {
                    this.IsFinished = true;
                    return;
                }

                planter.AssignColonist(this.Colonist.Id, endTile.Index);
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(endTile.X, endTile.Y, planter.MainTile.X, planter.MainTile.Y);
                this.CurrentAction = new ActionWalk(colonist, path, Vector2f.Zero, direction);
            }
            else
            {
                // Should be next to a planter.
                var planter = World.GetPlanters().Where(d => this.CanWork(d, this.Colonist.MainTile)).FirstOrDefault();
                if (planter == null)
                {
                    this.IsFinished = true;
                    return;
                }

                planter.AssignColonist(this.Colonist.Id, this.Colonist.MainTile.Index);
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(this.Colonist.MainTile.X, this.Colonist.MainTile.Y, planter.MainTile.X, planter.MainTile.Y);
                if (this.Colonist.Rotation.ApproxEquals(DirectionHelper.GetAngleFromDirection(direction), 0.001f))
                {
                    // In place and facing the right way.
                    this.CurrentAction = new ActionFarmPlant(colonist, planter);
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
            // Farming deselected?
            if (this.Colonist.WorkPriorities[ColonistPriority.FarmPlant] == 0 && this.CurrentAction is ActionWalk w) w.Path.RemainingNodes.Clear();

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

        private bool CanWork(IPlanter planter, ISmallTile tile)
        {
            if (!planter.GetAccessTiles(this.Colonist.Id).Contains(tile)) return false;
            if (planter.PlanterStatus == PlanterStatus.Dead || planter.RemoveCrop) return true;
            if (planter.PlanterStatus == PlanterStatus.WaitingForSeeds && (this.Colonist.WorkPriorities[ColonistPriority.FarmPlant] > 0 || planter.JobProgress > 0)) return true;
            return false;
        }
    }
}
