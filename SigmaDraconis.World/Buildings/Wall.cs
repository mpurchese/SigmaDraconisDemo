namespace SigmaDraconis.World.Buildings
{
    using System.Collections.Generic;
    using System.Linq;
    using Language;
    using ProtoBuf;
    using Shared;
    using Rooms;
    using WorldInterfaces;

    [ProtoContract]
    public class Wall : Building, IWall
    {
        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        [ProtoMember(2)]
        public int? RoomId { get; private set; }

        public Wall() : base(ThingType.Wall)
        {
            this.canRecycleReasonStringId = StringsForMouseCursor.SupportingRoof;
        }

        public Wall(ISmallTile mainTile, Direction direction) : base(ThingType.Wall, mainTile, new List<ISmallTile> { mainTile, mainTile.GetTileToDirection(direction) })
        {
            this.canRecycleReasonStringId = StringsForMouseCursor.SupportingRoof;
            this.Direction = direction;
        }

        public override bool CanRecycle()
        {
            // Can't deconstruct external wall, i.e. one with a roof in only one direction.  So we do an XOR here.
            if (this.mainTile.ThingsPrimary.Any(t => t.ThingType == ThingType.Roof) ^ this.mainTile.GetTileToDirection(this.Direction).ThingsPrimary.Any(t => t.ThingType == ThingType.Roof)) return false;
            return base.CanRecycle();
        }

        public override void AfterConstructionComplete()
        {
            base.AfterConstructionComplete();
            RoomManager.SplitRoom(this);
        }
    }
}
