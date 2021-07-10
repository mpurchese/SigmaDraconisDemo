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
    public class ActivitySleep : ActivityBase
    {
        [ProtoMember(1)]
        public int? PodId { get; private set; }

        // Deserialisation ctor
        protected ActivitySleep() { }

        public ActivitySleep(IColonist colonist, ISleepPod pod, Path path = null) : base(colonist, pod?.Id)
        {
            this.PodId = pod?.Id;
            UpdateCurrentAction(colonist, path);
            if (this.CurrentAction?.IsFinished != false) this.IsFinished = true;
        }

        private void UpdateCurrentAction(IColonist colonist, Path path = null)
        {
            if (this.CurrentAction is ActionSleep a && a.IsFinished) return;

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

                if (pod != null || colonist.Body.Energy <= 5)
                {
                    this.CurrentAction = new ActionSleep(colonist);
                }
                else
                {
                    // Won't sleep outside pod unless very tired
                    this.IsFinished = true;
                    return;
                }
            }
        }

        public override void Update()
        {
            if (this.CurrentAction is ActionWalk w && w.Path != null && this.PodId.HasValue)
            {
                // Pod may have been assigned to someone else
                var pod = World.GetThing(this.PodId.Value) as ISleepPod;
                if (pod == null || !pod.CanAssignColonist(this.colonistId)) w.Path.RemainingNodes.Clear();
            }

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
