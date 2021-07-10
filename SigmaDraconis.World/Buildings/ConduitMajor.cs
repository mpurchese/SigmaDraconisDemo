namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using WorldInterfaces;
    using Shared;

    [ProtoContract]
    public class ConduitMajor : Building, IConduitMajor
    {
        [ProtoMember(1)]
        public int Node1 { get; set; }

        [ProtoMember(2)]
        public int? Node2 { get; set; }

        public ConduitMajor() : base()
        {
        }

        public ConduitMajor(ISmallTile tile) : base(ThingType.ConduitMajor, tile, 1)
        {
        }
    }
}
