namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using WorldInterfaces;
    using Shared;

    [ProtoContract]
    public class ConduitMinor : Building, IConduitMinor
    {
        [ProtoMember(1)]
        public int ConnectedNodeId { get; set; }

        public ConduitMinor() : base()
        {
        }

        public ConduitMinor(ISmallTile tile) : base(ThingType.ConduitMinor, tile, 1)
        {
        }
    }
}
