namespace SigmaDraconis.AI
{
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.Rooms;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivitySeekSafeTemperature : ActivityBase
    {
        // Deserialisation ctor
        protected ActivitySeekSafeTemperature() { }

        public ActivitySeekSafeTemperature(IColonist colonist, Path path) : base(colonist)
        {
            this.CurrentAction = path != null ? new ActionWalk(colonist, path, Vector2f.Zero, Direction.None, 0.25f) as ActionBase : new ActionWait(this.Colonist);

            var endTile = path != null ? World.GetSmallTile(path.EndPosition) : this.Colonist.MainTile;
            if (endTile.ThingsPrimary.FirstOrDefault(t => t.ThingType == ThingType.SleepPod) is ISleepPod pod)
            {
                if (!pod.CanAssignColonist(this.Colonist.Id))
                {
                    this.IsFinished = true;
                    return;
                }

                pod.AssignColonist(this.Colonist.Id, endTile.Index);
            }
        }

        public override void Update()
        {
            if (this.CurrentAction?.IsFinished != false)
            {
                if (this.Colonist.Body.Temperature < 18 
                    && (this.Colonist.MainTile.HeatSources.Any() || this.Colonist.MainTile.ThingsPrimary.OfType<ISleepPod>().Any(t => t.IsHeaterSwitchedOn)))
                {
                    this.CurrentAction = new ActionWait(this.Colonist);
                }
                else if (this.Colonist.Body.Temperature > 22 && RoomManager.GetTileTemperature(this.Colonist.MainTileIndex, false) < 30)
                {
                    this.CurrentAction = new ActionWait(this.Colonist);
                }
                else
                {
                    this.IsFinished = true;
                    return;
                }
            }

            this.CurrentAction.Update();
        }
    }
}
