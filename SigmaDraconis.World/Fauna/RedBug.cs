namespace SigmaDraconis.World.Fauna
{
    using Shared;
    using WorldInterfaces;
    using ProtoBuf;

    [ProtoContract]
    public class RedBug : Bug
    {
        public RedBug() : base(ThingType.RedBug)
        {
        }

        public RedBug(ISmallTile tile) : base(ThingType.RedBug, tile)
        {
        }
    }
}
