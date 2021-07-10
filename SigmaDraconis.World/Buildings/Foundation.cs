namespace SigmaDraconis.World.Buildings
{
    using System;
    using System.Linq;
    using ProtoBuf;
    using Config;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class Foundation : Building, IFoundation
    {
        public Foundation() : base()
        {
        }

        public Foundation(ISmallTile mainTile, ThingType thingType) : base(thingType, mainTile, 1)
        {
        }

        public override void AfterAddedToWorld()
        {
            World.TilesWithGroundCoverRemovedQueue.Enqueue(new Tuple<int, int>(this.MainTileIndex, this.MainTile.GroundCoverDensity));
            this.MainTile.GroundCoverDensity = 0;
            EventManager.EnqueueWorldPropertyChangeEvent(this.MainTileIndex, nameof(ISmallTile.GroundCoverDensity));
            EventManager.EnqueueWorldPropertyChangeEvent(this.MainTileIndex, nameof(ISmallTile.IsMineResourceVisible), this.MainTile.Row);
            if (this.IsReady) this.AfterConstructionComplete();
            base.AfterAddedToWorld();
        }

        public override void AfterRemoveFromWorld()
        {
            EventManager.EnqueueWorldPropertyChangeEvent(this.MainTileIndex, nameof(ISmallTile.IsMineResourceVisible), this.MainTile.Row);
            base.AfterRemoveFromWorld();
        }

        public override bool CanRecycle()
        {
            // If recycling and there is a wall or a door, then must have foundation on the other side of the wall or door 
            foreach (var wall in this.MainTile.ThingsPrimary.OfType<IWall>())
            {
                if (this.MainTile.GetTileToDirection(wall.Direction).ThingsAll.All(t => !t.ThingType.IsFoundationLayer() || (t as IBuildableThing)?.IsReady != true))
                {
                    return false;
                }
            }

            var wallNW = this.MainTile.TileToNW.ThingsPrimary.OfType<IWall>().FirstOrDefault(t => t.Direction == Direction.SE);
            if (wallNW != null && this.MainTile.TileToNW.ThingsPrimary.OfType<Foundation>().All(t => !t.IsReady)) return false;

            var wallNE = this.MainTile.TileToNE.ThingsPrimary.OfType<IWall>().FirstOrDefault(t => t.Direction == Direction.SW);
            if (wallNE != null && this.MainTile.TileToNE.ThingsPrimary.OfType<Foundation>().All(t => !t.IsReady)) return false;

            return base.CanRecycle();
        }
    }
}
