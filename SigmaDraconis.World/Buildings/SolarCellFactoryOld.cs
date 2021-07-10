namespace SigmaDraconis.World.Buildings
{
    using System.Linq;
    using ProtoBuf;
    using WorldInterfaces;
    using Shared;

    [ProtoContract]
    public class SolarCellFactoryOld : Building, IRotatableThing
    {
        private int animationCheckTimer;

        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        public SolarCellFactoryOld() : base()
        {
        }

        public SolarCellFactoryOld(ISmallTile tile, Direction direction) : base(ThingType.SolarCellFactory, tile, 2)
        {
            this.Direction = direction;
        }

        public override void Update()
        {
            if (this.IsReady)
            {
                if (this.animationCheckTimer == 0)
                {
                    this.AnimationFrame = this.IsReady && World.GetThings<IBuildableThing>(ThingType.SolarPanelArray).Any(t => t.ConstructionProgress < 100 && !t.IsConstructionPaused) ? 4 : 3;
                    this.animationCheckTimer = 8;
                }
                else this.animationCheckTimer--;
            }
            else this.AnimationFrame = 1;

            base.Update();
        }

        public override bool CanRecycle()
        {
            // Can't recycle if a solar panel is under construction.
            return World.GetThings<IBuildableThing>(ThingType.SolarPanelArray).All(t => t.IsReady) && base.CanRecycle();
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
