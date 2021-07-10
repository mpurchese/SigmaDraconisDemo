namespace SigmaDraconis.World.Fauna
{
    using Shared;
    using WorldInterfaces;
    using ProtoBuf;

    [ProtoContract]
    [ProtoInclude(100, typeof(Bee))]
    public class FlyingInsect : Animal, IFlyingInsect
    {
        [ProtoMember(1)]
        public int Height { get; set; }

        [ProtoMember(2)]
        public int Speed { get; set; }

        [ProtoMember(3)]
        public float Angle { get; set; }

        public FlyingInsect() : base(ThingType.None)
        {
        }

        public FlyingInsect(ThingType type) : base(type)
        {
        }

        public FlyingInsect(ThingType type, ISmallTile tile) : base(type, tile)
        {
        }

        protected override void RaiseRendererUpdateEvent()
        {
            EventManager.MovedFlyingInsects.AddIfNew(this.Id);
        }
    }
}
