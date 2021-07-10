namespace SigmaDraconis.World.Flora
{
    using System.Collections.Generic;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using Rooms;
    using WorldInterfaces;

    /// <summary>
    /// Orangefruit
    /// </summary>
    [ProtoContract]
    public class SmallPlant12 : FruitPlant, IAnimatedThing
    {
        [ProtoMember(1)]
        private bool fruitFinished;

        [ProtoMember(2)]
        private int hoursSinceGrowth;

        public override bool CanFruit => !this.IsDead && !this.fruitFinished;
        public override int GrowthStage => this.animationFrame > 8 ? 3 : (this.animationFrame + 3) / 4;
        public override int MaxGrowthStage => 3;
        public override bool IsDead => this.AnimationFrame > 16;
        public override int CountFruitAvailable => this.AnimationFrame == 15 || this.AnimationFrame == 16 ? 1 : 0;

        public SmallPlant12() : base(ThingType.SmallPlant12)
        {
        }

        public SmallPlant12(ISmallTile tile, bool fromSeed = false) : base(ThingType.SmallPlant12, tile, 1)
        {
            if (fromSeed) this.AnimationFrame = Rand.Next(2) + 3;
            else
            {
                this.hoursSinceGrowth = 100;
                this.AnimationFrame = (Rand.Next(3) * 4) + Rand.Next(2) + 1;
            }
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var organics = 2;
            if (this.AnimationFrame >= 9 && this.AnimationFrame < 17) organics = 4;
            else if (this.AnimationFrame >= 5 && this.AnimationFrame < 9) organics = 3;

            var result = new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, organics },
                { ItemType.IronOre, 0 }
            };

            return result;
        }

        public override void RemoveFruit(bool leaveSeed)
        {
            if (this.AnimationFrame == 15 || this.AnimationFrame == 16)
            {
                this.AnimationFrame -= 4;
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

            // 1-2   Small, no leaves
            // 3-4   Small, leaves
            // 5-6   Med, no leaves
            // 7-8   Med, leaves
            // 9-10  Big, no leaves
            // 11-12 Big, leaves
            // 13-14 Big, unripe fruit
            // 15-16 Big, ripe fruit
            // 17-18 Big, dead

            // Growth controlled by light and temperature triggers.  Min. 100 hours between each growth step, so should only be 1 per day 
            this.hoursSinceGrowth++;
            var light = RoomManager.GetTileLightLevel(this.MainTileIndex);
            var temperature = RoomManager.GetTileTemperature(this.MainTileIndex);
            var hasFruit = this.CountFruitAvailable > 0;

            if (this.IsDead)
            {
                if (temperature < -10 - Rand.Next(10))
                {
                    World.RemoveThing(this);   
                }
            }
            else if (hasFruit)
            {
                if (temperature < 0 - Rand.Next(5))
                {
                    this.seedNextFrame = true;
                    this.AnimationFrame += 2;
                }
            }
            else if (temperature < 0 - Rand.Next(5))
            {
                if (this.fruitFinished && this.animationFrame.In(11, 12)) this.animationFrame += 6; 
                if (this.animationFrame.In(3, 4, 7, 8, 11, 12)) this.AnimationFrame -= 2;
                else if (this.animationFrame.In(13, 14)) this.animationFrame -= 4;
            }
            else if (temperature > Rand.Next(5))
            {
                if (light > 0.4 && this.animationFrame.In(1, 2, 5, 6))
                {
                    if (this.hoursSinceGrowth >= 100)
                    {
                        this.AnimationFrame += 6;
                        this.hoursSinceGrowth = 0;
                    }
                    else this.AnimationFrame += 2;
                }
                else if (light > 0.4 && this.animationFrame.In(9, 10))
                {
                    if (this.hoursSinceGrowth >= 100)
                    {
                        this.AnimationFrame += 4;
                        this.hoursSinceGrowth = 0;
                    }
                    else this.AnimationFrame += 2;
                }
                else if (this.animationFrame.In(13, 14) && this.hoursSinceGrowth > 70 + Rand.Next(10))
                {
                    this.AnimationFrame += 2;
                    World.HandleFruitPlantUpdate(this);
                }
            }

            // Remove any fruit harvest jobs if the fruit have gone
            if (hasFruit && this.CountFruitAvailable == 0)
            {
                this.HarvestFruitPriority = 0;
                World.HandleFruitPlantUpdate(this);
            }

            if (this.seedNextFrame)
            {
                this.seedNextFrame = false;
                foreach (var tile in this.MainTile.AdjacentTiles8)
                {
                    if (tile.TerrainType != TerrainType.Dirt || tile.BiomeType != BiomeType.Wet || Rand.NextDouble() > 0.2) continue;

                    if (seedTiles == null) seedTiles = new List<int> { tile.Index };
                    else seedTiles.Add(tile.Index);
                }
            }

            return seedTiles;
        }
    }
}
