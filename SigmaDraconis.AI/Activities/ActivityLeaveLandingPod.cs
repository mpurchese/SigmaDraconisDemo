namespace SigmaDraconis.AI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivityLeaveLandingPod : ActivityBase
    {
        // Deserialisation ctor
        protected ActivityLeaveLandingPod() { }

        public ActivityLeaveLandingPod(IColonist colonist) : base(colonist)
        {
            this.CurrentAction = new ActionWait(colonist, 180);
        }

        public override void Update()
        {
            if (this.CurrentAction?.IsFinished != false)
            {
                if (this.CurrentAction is ActionWait)
                {
                    if (!(this.Colonist.MainTile.ThingsPrimary.FirstOrDefault(t => t.ThingType == ThingType.LandingPod) is ILandingPod pod))
                    {
                        this.Colonist.IsArrived = true;
                        this.IsFinished = true;
                        return;
                    }

                    if (pod.AnimationFrame < 9)
                    {
                        this.CurrentAction = new ActionWait(this.Colonist, 60);
                        return;
                    }

                    var tileToS = this.Colonist.MainTile.TileToS;
                    var node1 = new PathNode(this.Colonist.MainTile.X, this.Colonist.MainTile.Y, Direction.S, Direction.N);
                    var node2 = new PathNode(tileToS.X, tileToS.Y, Direction.S, Direction.N);
                    var nodeStack = new Stack<PathNode>();
                    nodeStack.Push(node2);
                    nodeStack.Push(node1);
                    var path = new Path(this.Colonist.MainTile.TerrainPosition, tileToS.TerrainPosition, nodeStack);
                    this.CurrentAction = new ActionWalk(this.Colonist, path, Vector2f.Zero, Direction.S);
                    EventManager.EnqueueColonistEvent(ColonistEventType.Arrival, this.colonistId);
                    if (World.WorldTime.TotalHoursPassed < Constants.ColonistNewColonyBonusHours - 1) this.Colonist.IsArrived = true;

                    // Prod up to two other colonists to welcome us to the colony
                    foreach (var otherColonist in World.GetThings<IColonist>(ThingType.Colonist)
                        .Where(c => c.Id != this.colonistId && !c.IsDead && !c.Body.IsSleeping)
                        .OrderBy(c => Guid.NewGuid())
                        .Take(2))
                    {
                        otherColonist.ColonistToWelcome = this.colonistId;
                    }
                }
                else
                {
                    this.Colonist.IsArrived = true;
                    this.IsFinished = true;     
                    return;
                }
            }

            this.CurrentAction.Update();
        }
    }
}
