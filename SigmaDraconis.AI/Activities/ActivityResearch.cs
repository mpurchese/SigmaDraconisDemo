namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivityResearch : ActivityBase
    {
        [ProtoMember(1)]
        public int LabId { get; private set; }

        // Deserialisation ctor
        protected ActivityResearch() { }

        public ActivityResearch(IColonist colonist, ILab lab, Path path = null) : base(colonist, lab.Id)
        {
            this.LabId = lab.Id;
            this.ResetMovement();
            UpdateCurrentAction(colonist, path);
            if (this.CurrentAction?.IsFinished != false) this.IsFinished = true;
        }

        public override void AfterDeserialization()
        {
            // Sanity check
            var lab = World.GetThing(this.LabId) as ILab;
            if (lab == null || !lab.CanAssignColonist(colonistId)) this.IsFinished = true;

            base.AfterDeserialization();
            this.Colonist.TargetBuilingID = lab.Id;
        }

        private void UpdateCurrentAction(IColonist colonist, Path path = null)
        {
            if (this.Colonist.WorkPriorities[ColonistPriority.ResearchBotanist] == 0 && this.Colonist.WorkPriorities[ColonistPriority.ResearchEngineer] == 0 && this.Colonist.WorkPriorities[ColonistPriority.ResearchGeologist] == 0)
            {
                this.IsFinished = true;
                return;
            }

            // If we were given a path, then start moving.  If not then we are already next to a lab.
            if (path != null)
            {
                // Find the lab that is next to the end tile
                var endTile = World.GetSmallTile(path.EndPosition);
                if (!(World.GetThing(this.LabId) is ILab lab))
                {
                    this.IsFinished = true;
                    return;
                }

                lab.AssignColonist(this.Colonist.Id, endTile.Index);
                var positionOffset = new Vector2f(0.2f * (lab.MainTile.TerrainPosition.X - endTile.TerrainPosition.X), 0.2f * (lab.MainTile.TerrainPosition.Y - endTile.TerrainPosition.Y));
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(endTile.X, endTile.Y, lab.MainTile.X, lab.MainTile.Y);
                this.CurrentAction = new ActionWalk(colonist, path, positionOffset, direction);
            }
            else
            {
                // Should be next to a lab.
                if (!(World.GetThing(this.LabId) is ILab lab))
                {
                    this.IsFinished = true;
                    return;
                }

                lab.AssignColonist(this.Colonist.Id, this.Colonist.MainTile.Index);
                var direction = DirectionHelper.GetDirectionFromAdjacentPositions(this.Colonist.MainTile.X, this.Colonist.MainTile.Y, lab.MainTile.X, lab.MainTile.Y);
                var position = new Vector2f((this.Colonist.MainTile.X + (0.2f * lab.MainTile.X)) / 1.2f, (this.Colonist.MainTile.Y + (0.2f * lab.MainTile.Y)) / 1.2f);
                if (this.Colonist.Rotation.ApproxEquals(DirectionHelper.GetAngleFromDirection(direction), 0.001f)
                    && (this.Colonist.Position - position).Length() < 0.1f)
                {
                    // In place and facing the right way.
                    this.CurrentAction = new ActionResearch(colonist, lab);
                }
                else
                {
                    // Make an empty path so that we can move to the required rotation and offset
                    this.BuildWalkActionForFacingTarget(direction, new Vector2f(0.2f * (lab.MainTile.X - this.Colonist.MainTile.X), 0.2f * (lab.MainTile.Y - this.Colonist.MainTile.Y)));
                }
            }
        }

        public override void Update()
        {
            // Research deselected?
            if (this.CurrentAction is ActionWalk w)
            {
                if (this.Colonist.WorkPriorities[ColonistPriority.ResearchBotanist] == 0 && this.Colonist.WorkPriorities[ColonistPriority.ResearchEngineer] == 0 && this.Colonist.WorkPriorities[ColonistPriority.ResearchGeologist] == 0)
                {
                    w.Path.RemainingNodes.Clear();
                }
                else
                {
                    if (!(World.GetThing(this.LabId) is ILab lab) || !lab.CanAssignColonist(this.colonistId))
                    {
                        w.Path.RemainingNodes.Clear();
                    }
                }
            }

            if (this.CurrentAction?.IsFinished != false)
            {
                if (this.CurrentAction?.IsFailed == true)
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
            if (this.CurrentAction.IsFinished)
            {
                this.IsFinished = true;
                return;
            }
            else if (this.CurrentAction is ActionWalk aw)
            {
                if (World.GetThing(this.LabId) is ILab lab) lab.AssignedColonistDistance = aw.Path.RemainingNodes.Count;
            }
            else if (World.GetThing(this.LabId) is ILab lab)
            {
                lab.AssignedColonistDistance = 0;
            }
        }
    }
}
