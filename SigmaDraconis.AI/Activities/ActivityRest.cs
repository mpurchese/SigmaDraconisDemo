namespace SigmaDraconis.AI
{
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.PathFinding;
    using WorldInterfaces;

    // Currently not used
    [ProtoContract]
    public class ActivityRest : ActivityBase
    {
        [ProtoMember(1, IsRequired = true)]
        private readonly bool isPod;

        // Deserialisation ctor
        protected ActivityRest() { }

        public ActivityRest(IColonist colonist, Path path = null) : base(colonist)
        {
            var tile = path != null ? World.GetSmallTile(path.EndPosition) : colonist.MainTile;
            this.isPod = tile.ThingsAll.Any(t => t.ThingType == ThingType.SleepPod);

            UpdateCurrentAction(colonist, path);
            if (this.CurrentAction?.IsFinished != false) this.IsFinished = true;
        }

        private void UpdateCurrentAction(IColonist colonist, Path path = null)
        {
            if (this.CurrentAction is ActionRest a && a.IsFinished) return;

            // If we were given a path, then start moving.  If not then we are already in a pod.
            if (path != null)
            {
                var endTile = World.GetSmallTile(path.EndPosition);
                if (endTile.ThingsPrimary.FirstOrDefault(t => t.ThingType == ThingType.SleepPod) is ISleepPod pod)
                {
                    if (!pod.CanAssignColonist(this.Colonist.Id))
                    {
                        this.IsFinished = true;
                        return;
                    }

                    pod.AssignColonist(this.Colonist.Id, endTile.Index);
                }

                this.CurrentAction = new ActionWalk(colonist, path, Vector2f.Zero);
            }
            else
            {
                var pod = this.Colonist.MainTile.ThingsPrimary.FirstOrDefault(t => t.ThingType == ThingType.SleepPod) as ISleepPod;
                if (pod != null)
                {
                    if (!pod.CanAssignColonist(this.Colonist.Id))
                    {
                        this.IsFinished = true;
                        return;
                    }

                    pod.AssignColonist(this.Colonist.Id, this.Colonist.MainTileIndex);
                }

                if (pod != null || !this.isPod)
                {
                    this.CurrentAction = new ActionRest(colonist);
                }
                else
                {
                    // Expecting to be in a pod, but for some reason we are not.  Probably we want to try again.
                    this.IsFinished = true;
                    return;
                }
            }
        }

        public override void Update()
        {
            if (this.CurrentAction?.IsFinished != false)
            {
                if (this.CurrentAction?.IsFailed != false)
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
