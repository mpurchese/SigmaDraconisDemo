namespace SigmaDraconis.World.Buildings
{
    using System.Linq;
    using ProtoBuf;
    using WorldInterfaces;
    using Shared;

    [ProtoContract]
    public class SoilSynthesiser : Building, IRotatableThing
    {
        private int animationCheckTimer;

        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        public SoilSynthesiser() : base()
        {
        }

        public SoilSynthesiser(ISmallTile tile, Direction direction) : base(ThingType.SoilSynthesiser, tile, 1)
        {
            this.Direction = direction;
        }

        public override void Update()
        {
            if (this.IsReady)
            {
                if (this.animationCheckTimer == 0)
                {
                    this.AnimationFrame = this.IsReady && World.GetPlanters().Any(t => t.ConstructionProgress < 100 && !t.IsConstructionPaused) ? 2 : 1;
                    this.animationCheckTimer = 8;
                }
                else this.animationCheckTimer--;
            }
            else this.AnimationFrame = 1;

            base.Update();
        }

        public override bool CanRecycle()
        {
            // Can't recycle if a planter is under construction.
            return World.GetThings<IBuildableThing>(ThingType.PlanterStone).All(t => t.IsReady) && base.CanRecycle();
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
