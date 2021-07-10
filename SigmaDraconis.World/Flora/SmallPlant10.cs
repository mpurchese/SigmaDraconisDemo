namespace SigmaDraconis.World.Flora
{
    using System;
    using System.Collections.Generic;
    using ProtoBuf;
    using Shared;
    using Rooms;
    using WorldInterfaces;

    /// <summary>
    /// Wormtail
    /// </summary>
    [ProtoContract]
    public class SmallPlant10 : Plant, IAnimatedThing
    {
        [ProtoMember(1)]
        private int growth;

        [ProtoMember(2)]
        private readonly int minTemp;

        public override int GrowthStage => Math.Min(4, (this.AnimationFrame + 11) / 12);
        public override int MaxGrowthStage => 4;
        public override bool HasDeadFrame => true;
        public override bool IsDead => this.animationFrame > 48;

        public SmallPlant10() : base(ThingType.SmallPlant10)
        {
        }

        public SmallPlant10(ISmallTile tile, bool fromSeed = false) : base(ThingType.SmallPlant10, tile, 1)
        {
            this.minTemp = Rand.Next(4) + 8;

            // Face away from water
            var f = Rand.Next(3) - 1;
            if (tile.TileToNE?.TerrainType == TerrainType.Coast)
            {
                if (f == -1) f = 11;
            }
            else if (tile.TileToNW?.TerrainType == TerrainType.Coast) f += 3;
            else if (tile.TileToSW?.TerrainType == TerrainType.Coast) f += 6;
            else if (tile.TileToSE?.TerrainType == TerrainType.Coast) f += 9;
            else if (tile.TileToSE?.TerrainType == TerrainType.Coast) f += 9;
            else if (tile.TileToN?.TerrainType == TerrainType.Coast) f = Rand.Next(2) + 1;
            else if (tile.TileToW?.TerrainType == TerrainType.Coast) f = Rand.Next(2) + 4;
            else if (tile.TileToS?.TerrainType == TerrainType.Coast) f = Rand.Next(2) + 7;
            else if (tile.TileToE?.TerrainType == TerrainType.Coast) f = Rand.Next(2) + 10;

            if (fromSeed) this.AnimationFrame = f + 1;
            else
            {
                this.AnimationFrame = (Rand.Next(5) * 12) + f + 1;
                this.growth = 80;
            }
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var organics = 1;
            if (this.AnimationFrame > 48) organics = 3;
            else if (this.AnimationFrame > 36) organics = 5;
            else if (this.AnimationFrame > 24) organics = 3;
            else if (this.AnimationFrame > 12) organics = 2;

            return new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, organics },
                { ItemType.IronOre, 0 }
            };
        }

        // This is called every hour
        public override List<int> UpdateGrowth()
        {
            base.UpdateGrowth();

            this.growth++;
            if (this.growth < 80) return null;

            var temperature = RoomManager.GetTileTemperature(this.MainTileIndex);
            var grow = temperature >= this.minTemp + 10 || (temperature >= this.minTemp && Rand.Next(10) == 0);
            if (!grow) return null;

            this.growth = 0;
            if (this.animationFrame >= 48) World.RemoveThing(this);
            else this.animationFrame += 8;

            return this.SpreadSeed();
        }

        private List<int> SpreadSeed()
        {
            var seedTiles = new List<int>();
            var i = 0;
            foreach (var tile in this.MainTile.AdjacentTiles8)
            {
                if (tile.BigTile.TerrainType == TerrainType.Coast && Rand.NextDouble() > (i < 4 ? 0.66 : 0.5))
                {
                    seedTiles.Add(tile.Index);
                }

                i++;
            }

            if (Rand.NextDouble() > 0.75) seedTiles.Add(this.mainTile.Index);

            return seedTiles;
        }
    }
}
