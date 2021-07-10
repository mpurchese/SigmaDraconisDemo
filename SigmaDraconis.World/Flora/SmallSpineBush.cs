namespace SigmaDraconis.World.Flora
{
    using System.Collections.Generic;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class SmallSpineBush : Plant, IAnimatedThing
    {
        public SmallSpineBush() : base(ThingType.SmallSpineBush)
        {
        }

        public SmallSpineBush(ISmallTile mainTile) : base(ThingType.SmallSpineBush, mainTile, 1)
        {
            this.AnimationFrame = Rand.Next(4) + 1;
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            return new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, 5 },
                { ItemType.IronOre, 0 }
            };
        }
    }
}
