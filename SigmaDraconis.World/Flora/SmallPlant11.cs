namespace SigmaDraconis.World.Flora
{
    using System;
    using System.Collections.Generic;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    /// <summary>
    /// Dragontail
    /// </summary>
    [ProtoContract]
    public class SmallPlant11 : Plant, IAnimatedThing
    {
        public override int GrowthStage => Math.Min(4, (this.AnimationFrame + 3) / 4);
        public override int MaxGrowthStage => 4;
        public override bool HasDeadFrame => true;
        public override bool IsDead => this.animationFrame > 16;

        public SmallPlant11() : base(ThingType.SmallPlant11)
        {
        }

        public SmallPlant11(ISmallTile tile) : base(ThingType.SmallPlant11, tile, 1)
        {
            // Face away from water
            var f = 4;
            if (tile.TileToNE?.TerrainType == TerrainType.Coast) f = 1;
            else if (tile.TileToNW?.TerrainType == TerrainType.Coast) f = 2;
            else if (tile.TileToSW?.TerrainType == TerrainType.Coast) f = 3;

            if (Rand.Next(2) == 0) this.AnimationFrame = 12 + f;
            else this.AnimationFrame = (Rand.Next(5) * 4) + f;
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var organics = 4;
            if (this.AnimationFrame > 16) organics = 6;
            else if (this.AnimationFrame > 12) organics = 8;
            else if (this.AnimationFrame > 8) organics = 6;
            else if (this.AnimationFrame > 4) organics = 5;

            return new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, organics },
                { ItemType.IronOre, 0 }
            };
        }
    }
}
