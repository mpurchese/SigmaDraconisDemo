namespace SigmaDraconis.World.Flora
{
    using ProtoBuf;
    using Shared;

    [ProtoContract]
    public class PlantSeed
    {
        [ProtoMember(1)]
        public long NextUpdateFrame { get; set; }

        [ProtoMember(2)]
        public ThingType ThingType { get; set; }

        [ProtoMember(3)]
        public int TileIndex { get; set; }

        [ProtoMember(4)]
        public int GrowthAttemptCount { get; set; }
    }
}
