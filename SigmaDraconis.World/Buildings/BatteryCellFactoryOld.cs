namespace SigmaDraconis.World.Buildings
{
    using System.Linq;
    using ProtoBuf;
    using WorldInterfaces;
    using Shared;

    [ProtoContract]
    public class BatteryCellFactoryOld : Building, IRotatableThing
    {
        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        public BatteryCellFactoryOld() : base()
        {
        }

        public BatteryCellFactoryOld(ISmallTile tile, Direction direction) : base(ThingType.BatteryCellFactory, tile, 1)
        {
            this.Direction = direction;
        }

        public override void Update()
        {
            if (this.IsReady) this.AnimationFrame = World.GetThings<IBuildableThing>(ThingType.Battery).Any(t => t.ConstructionProgress < 100 && !t.IsConstructionPaused) ? 4 : 3;
            else this.AnimationFrame = 1;

            base.Update();
        }

        public override bool CanRecycle()
        {
            // Can't recycle if a solar panel is under construction.
            return World.GetThings<IBuildableThing>(ThingType.Battery).All(t => t.IsReady) && base.CanRecycle();
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{base.GetTextureName()}_{this.Direction.ToString()}";
        }

        protected override void BeforeRecycle()
        {
            this.AnimationFrame = 1;
            base.BeforeRecycle();
        }
    }
}
