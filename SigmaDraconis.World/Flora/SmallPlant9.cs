namespace SigmaDraconis.World.Flora
{
    using System.Collections.Generic;
    using ProtoBuf;
    using Shared;
    using Rooms;
    using WorldInterfaces;

    /// <summary>
    /// Pinkfruit
    /// </summary>
    [ProtoContract]
    public class SmallPlant9 : FruitPlant, IAnimatedThing
    {
        [ProtoMember(1)]
        public int ProgressToNextFrame { get; private set; }

        [ProtoMember(2)]
        private readonly int minTemperature;

        [ProtoMember(3)]
        private readonly double minLight;

        [ProtoMember(4)]
        private bool fruitFinished;

        public override bool CanFruit => !this.IsDead && !this.fruitFinished;
        public override int GrowthStage => (this.AnimationFrame + 1) / 2;
        public override int MaxGrowthStage => 8;
        public override bool IsDead => this.animationFrame > 14;

        public override int CountFruitAvailable => this.AnimationFrame == 13 || this.AnimationFrame == 14 ? 1 : 0;

        public SmallPlant9() : base(ThingType.SmallPlant9)
        {
        }

        public SmallPlant9(ISmallTile tile, bool fromSeed = false) : base(ThingType.SmallPlant9, tile, 1)
        {
            if (fromSeed) this.AnimationFrame = Rand.Next(2) + 1;
            else
            {
                // From world generator.
                this.AnimationFrame = Rand.Next(12) + 1;
                this.ProgressToNextFrame = Rand.Next(100);
            }

            this.minLight = (Rand.NextDouble() * 0.25) + 0.1;
            this.minTemperature = Rand.Next(4) - 2;
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var biomass = 1;
            if (this.AnimationFrame >= 7) biomass = 3;
            else if (this.AnimationFrame >= 5) biomass = 2;

            var result = new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, biomass },
                { ItemType.IronOre, 0 }
            };

            return result;
        }

        public override void RemoveFruit(bool leaveSeed)
        {
            if (this.AnimationFrame == 13 || this.AnimationFrame == 14)
            {
                this.AnimationFrame -= 6;
                this.fruitFinished = true;
                World.HandleFruitPlantUpdate(this);
            }

            base.RemoveFruit(leaveSeed);
        }

        // This is called every hour
        public override List<int> UpdateGrowth()
        {
            List<int> seedTiles = null;
            base.UpdateGrowth();

            // Frame 1-2: Young 1
            // Frame 3-4: Young 2
            // Frame 5-6: Young 3
            // Frame 7-8: Mature
            // Frame 9-10: Flowers
            // Frame 11-12: Unripe fruit
            // Frame 13-14: Ripe fruit
            // Frame 15-16: Dead

            // Growth controlled by light and temperature triggers
            var light = RoomManager.GetTileLightLevel(this.MainTileIndex);
            var temperature = RoomManager.GetTileTemperature(this.MainTileIndex);

            var minTemp = this.CountFruitAvailable > 0 ? -99 : this.minTemperature;
            if (light >= this.minLight && temperature >= minTemp && !this.IsDead)
            {
                this.ProgressToNextFrame++;
                if (this.ProgressToNextFrame >= 100)
                {
                    this.ProgressToNextFrame = 0;
                    if (this.fruitFinished) this.AnimationFrame += 8;  // Now dead
                    else this.AnimationFrame += 2;

                    if (this.animationFrame >= 13)
                    {
                        World.HandleFruitPlantUpdate(this);
                        if (this.animationFrame >= 15) this.seedNextFrame = true;
                    }
                }
            }
            else if (this.IsDead && ((temperature <= -10 && Rand.Next(10) == 0) || (temperature <= -18 && Rand.Next(2) == 0)))
            {
                World.RemoveThing(this);
            }

            // Remove any fruit harvest jobs if the fruit have gone
            if (this.HarvestFruitPriority != WorkPriority.Disabled && this.CountFruitAvailable == 0)
            {
                this.HarvestFruitPriority = 0;
                World.HandleFruitPlantUpdate(this);
            }

            if (this.seedNextFrame)
            {
                this.seedNextFrame = false;
                foreach (var tile in this.MainTile.AdjacentTiles8)
                {
                    if (tile.TerrainType != TerrainType.Dirt) continue;
                    if (tile.BiomeType != BiomeType.SmallPlants && tile.BiomeType != BiomeType.Wet && tile.BiomeType != BiomeType.Grass) continue;
                    if (Rand.NextDouble() > 0.2) continue;

                    if (seedTiles == null) seedTiles = new List<int> { tile.Index };
                    else seedTiles.Add(tile.Index);
                }
            }

            return seedTiles;
        }
    }
}
