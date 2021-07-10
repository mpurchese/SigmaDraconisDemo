namespace SigmaDraconis.World.Fauna
{
    using Shared;
    using WorldInterfaces;
    using ProtoBuf;

    [ProtoContract]
    public class BlueBug : Bug
    {
        public BlueBug() : base(ThingType.BlueBug)
        {
        }

        public BlueBug(ISmallTile tile) : base(ThingType.BlueBug, tile)
        {
        }
    }
}
