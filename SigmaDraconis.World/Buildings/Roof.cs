namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using Shared;
    using Rooms;
    using WorldInterfaces;

    [ProtoContract]
    public class Roof : Building
    {
        public override bool CanWalk => true;

        public Roof() : base(ThingType.Roof)
        {
        }

        public Roof(ISmallTile mainTile) : base(ThingType.Roof, mainTile, 1)
        {
        }

        public override void Update()
        {
            // If there is still a room here then remove it.  Causes the lights to go out, room controls to be disabled, renderers to update.
            if (this.IsDesignatedForRecycling && !this.IsRecycling)
            {
                RoomManager.RemoveRoom(this.MainTileIndex);
            }

            base.Update();
        }
    }
}
