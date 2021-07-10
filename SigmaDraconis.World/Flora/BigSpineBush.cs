namespace SigmaDraconis.World.Flora
{
    using System.Collections.Generic;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class BigSpineBush : Plant, IAnimatedThing
    {
        public BigSpineBush() : base(ThingType.BigSpineBush)
        {
        }

        public BigSpineBush(ISmallTile mainTile) : base(ThingType.BigSpineBush, mainTile, 2)
        {
            this.AnimationFrame = Rand.Next(4) + 1;
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            return new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, 18 },
                { ItemType.IronOre, 0 }
            };
        }
    }
}
