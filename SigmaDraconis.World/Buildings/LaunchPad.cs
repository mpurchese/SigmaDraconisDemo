namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class LaunchPad : Building
    {
        public LaunchPad() : base(ThingType.LaunchPad)
        {
        }

        public LaunchPad(ISmallTile mainTile) : base(ThingType.LaunchPad, mainTile, 5)
        {
        }

        // We have TileBlockModel.None to allow colonists to cut the corner, but that doesn't mean they should cut right across the middle.
        public override bool CanWalk => false;
    }
}
