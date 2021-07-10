namespace SigmaDraconis.World.Buildings
{
    using System.Linq;
    using ProtoBuf;
    using WorldInterfaces;
    using Shared;

    [ProtoContract]
    public class GlassFactoryOld : Building, IRotatableThing
    {
        private int animationCheckTimer;

        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        [ProtoMember(2)]
        public double MaintenanceLevel { get; set; }

        [ProtoMember(3)]
        public int RepairPriority { get; set; }

        public GlassFactoryOld() : base()
        {
        }

        public GlassFactoryOld(ISmallTile tile, Direction direction) : base(ThingType.GlassFactory, tile, 1)
        {
            this.Direction = direction;
        }

        public override void AfterConstructionComplete()
        {
            this.RepairPriority = 2;
            this.MaintenanceLevel = 1.0;
            base.AfterConstructionComplete();
        }

        public override void Update()
        {
            if (this.IsReady)
            {
                if (this.animationCheckTimer == 0)
                {
                    this.AnimationFrame = this.IsReady && World.GetThings<IBuildableThing>(ThingType.Wall, ThingType.Roof, ThingType.Door).Any(t => t.ConstructionProgress < 100 && !t.IsConstructionPaused) ? 2 : 1;
                    this.animationCheckTimer = 8;
                }
                else this.animationCheckTimer--;
            }
            else this.AnimationFrame = 1;

            base.Update();
        }

        public override bool CanRecycle()
        {
            // Can't recycle if glass stuff is under construction.
            return World.GetThings<IBuildableThing>(ThingType.Wall, ThingType.Roof, ThingType.Door).All(t => t.IsReady) && base.CanRecycle();
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{base.GetTextureName()}_{this.Direction.ToString()}";
        }
    }
}
