namespace SigmaDraconis.World.Flora
{
    using System;
    using System.Collections.Generic;
    using ProtoBuf;
    using Shared;
    using Rooms;
    using WorldInterfaces;

    /// <summary>
    /// Redfruit
    /// </summary>
    [ProtoContract]
    public class SmallPlant6 : FruitPlant, IAnimatedThing, IThingHidesTiles
    {
        [ProtoMember(2)]
        public int Growth { get; private set; }

        [ProtoMember(4)]
        private readonly int minTemp;

        [ProtoMember(5)]
        private readonly double minLight;

        public override int CountFruitAvailable
        {
            get
            {
                switch (this.AnimationFrame % 9)
                {
                    case 7: return 2;
                    case 8: return 1;
                }

                return 0;
            }
        }

        public override int GrowthStage => this.AnimationFrame % 9 == 0 ? 5 : Math.Min(5, this.AnimationFrame % 9);
        public override int MaxGrowthStage => 5;
        public override bool CanFruitUnripe => true;
        public override bool HasFruitUnripe => this.AnimationFrame % 9 == 6;
        public override bool HasDeadFrame => true;
        public override bool IsDead => this.AnimationFrame % 9 == 0;

        public SmallPlant6() : base(ThingType.SmallPlant6)
        {
        }

        public SmallPlant6(ISmallTile tile, bool fromSeed = false) : base(ThingType.SmallPlant6, tile, 1)
        {
            if (fromSeed) this.AnimationFrame = 1 + (9 * Rand.Next(4));
            else
            {
                // From world generator.
                if (Rand.Next(2) == 0)
                {
                    this.Growth = 70 + Rand.Next(5);
                    this.AnimationFrame = 3 + (9 * Rand.Next(4));
                }
                else
                {
                    this.Growth = 150 + Rand.Next(5);
                    this.AnimationFrame = 5 + (9 * Rand.Next(4));
                }
            }

            this.minTemp = 5 + Rand.Next(4);
            this.minLight = (Rand.NextDouble() * 0.25) + 0.1;
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var biomass = 2;
            if (this.AnimationFrame % 9 == 0) biomass = 5;   // Dead
            else if (this.Growth >= 150) biomass = 10;
            else if (this.Growth >= 110) biomass = 8;
            else if (this.Growth >= 70) biomass = 6;
            else if (this.Growth >= 30) biomass = 4;

            var result = new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, biomass },
                { ItemType.IronOre, 0 }
            };

            return result;
        }

        public IEnumerable<ISmallTile> GetHiddenTiles()
        {
            if (this.Growth < 70) yield break;

            var tile = this.mainTile.TileToN;
            if (tile == null) yield break;
            yield return tile;
        }

        public override void RemoveFruit(bool leaveSeed)
        {
            switch (this.AnimationFrame % 9)
            {
                case 7: this.AnimationFrame++; World.HandleFruitPlantUpdate(this); break;
                case 8: this.AnimationFrame -= 3; World.HandleFruitPlantUpdate(this); break;
            }

            base.RemoveFruit(leaveSeed);
        }

        // This is called every hour
        public override List<int> UpdateGrowth()
        {
            List<int> seedTiles = null;
            base.UpdateGrowth();

            // Lifecycle:
            // Growth happens when light and temperature above about 5C.
            // 1. Seeds germinate when light and temperature > 5C and hour < 90.  Small plant frame 1.  Hour approx 70.  Growth = 0.
            // 2. Gets bigger after 30 hours.  Frame 2.  Hour approx 100.  Growth = 30.
            // 3. Gets bigger after 40 hours.  Frame 3.  Hour approx 140.  Growth = 70.
            // 4. Gets bigger after 40 hours.  Frame 4.  Hour approx 105 day 2.  Growth = 110.
            // 5. Gets bigger after 40 hours.  Frame 5.  Hour approx 145 day 2 or 70 day 3.  Growth = 150.
            // 6. Green fruit after 40 hours.  Frame 6.  Hour approx 110 day 3.  Growth = 190.
            // 7. Red fruit after 20 hours.  Frame 7.  Hour approx 130 day 3.  Growth = 210.
            // 5. Dies when dark and cold after fruiting.  Frame 9.  Hour approx 180 day 3.
            // 11. When temperature drops below -5 to -10, plant is removed.  Early day 4.  Seed added in place.

            // Growth controlled by light and temperature triggers
            var light = RoomManager.GetTileLightLevel(this.MainTileIndex);
            var temperature = RoomManager.GetTileTemperature(this.MainTileIndex);

            if (light > this.minLight && temperature >= this.minTemp)
            {
                this.Growth++;
                if (this.Growth == 30 || this.Growth == 70 || this.Growth == 110 || this.Growth == 150 || this.Growth == 190 || this.Growth == 210)
                {
                    this.AnimationFrame++;
                }
            }
            else
            {
                if (this.Growth >= 190)
                {
                    if (temperature < this.minTemp && this.AnimationFrame % 9 != 0)
                    {
                        // Brown leaves frames 9, 18, 27, 36.  Fruit go, seed added.
                        this.AnimationFrame = 9 + (((this.AnimationFrame - 1) / 9) * 9);
                        seedTiles = new List<int> { this.MainTileIndex };
                    }
                    else if (temperature <= this.minTemp - 5 && Rand.NextDouble() > 0.85 && this.RecycleProgress == 0)
                    {
                        // Disappear in the cold and dark and leave a seed
                        World.RemoveThing(this);
                    }
                }
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
                if (seedTiles == null) seedTiles = new List<int> { this.MainTileIndex };
            }

            return seedTiles;
        }
    }
}
