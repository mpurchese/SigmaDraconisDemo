namespace SigmaDraconis.World.Fauna
{
    using Shared;
    using PathFinding;
    using WorldInterfaces;
    using ProtoBuf;

    [ProtoContract]
    [ProtoInclude(100, typeof(Fish))]
    public class WaterAnimal : Animal, IWaterAnimal
    {
        [ProtoMember(1)]
        public int Speed { get; set; }

        [ProtoMember(2)]
        public float Angle { get; set; }

        [ProtoMember(3)]
        public long CreatedFrame { get; set; }

        [ProtoMember(4)]
        public Path Path { get; set; }

        protected WaterAnimal() : base(ThingType.None)
        {
        }

        protected WaterAnimal(ThingType type) : base(type)
        {
        }

        public WaterAnimal(ThingType type, ISmallTile tile) : base(type, tile)
        {
            this.CreatedFrame = World.WorldTime.FrameNumber;
        }
    }
}
