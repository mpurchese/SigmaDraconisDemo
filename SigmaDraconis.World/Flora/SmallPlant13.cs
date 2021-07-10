namespace SigmaDraconis.World.Flora
{
    using System.Collections.Generic;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;

    /// <summary>
    /// Coal fungus
    /// </summary>
    [ProtoContract]
    public class SmallPlant13 : Plant, IAnimatedThing, IRenderOffsettable, IPositionOffsettable
    {
        [ProtoMember(1)]
        public Vector2f PositionOffset { get; protected set; }

        public override int GrowthStage => (this.AnimationFrame + 3) / 4;
        public override int MaxGrowthStage => 4;

        public SmallPlant13() : base(ThingType.SmallPlant13)
        {
        }

        public SmallPlant13(ISmallTile tile) : base(ThingType.SmallPlant13, tile, 1)
        {
            var density = ((int)tile.MineResourceDensity).Clamp(1, 4);
            this.AnimationFrame = Rand.Next(density * 4) + 1;

            var offsetX = (float)(Rand.NextDouble() * 0.3f) - 0.15f;
            var offsetY = (float)(Rand.NextDouble() * 0.3f) - 0.15f;
            this.PositionOffset = new Vector2f(offsetX, offsetY);
            this.RenderPositionOffset = new Vector2i((int)((10.6666667 * offsetX) + (10.6666667 * offsetY)), (int)((5.3333333 * offsetY) - (5.3333333 * offsetX)));
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            return new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, this.GrowthStage + 1 },
                { ItemType.IronOre, 0 }
            };
        }
    }
}
