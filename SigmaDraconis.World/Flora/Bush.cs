namespace SigmaDraconis.World.Flora
{
    using System;
    using System.Collections.Generic;
    using ProtoBuf;
    using Rooms;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class Bush : FruitPlant, IThingWithShadow, IAnimatedThing
    {
        [ProtoMember(2)]
        public int Growth { get; protected set; }

        [ProtoMember(3)]
        public bool Seeded { get; protected set; }

        [ProtoMember(4)]
        private readonly int minTemp;

        [ProtoMember(5)]
        private readonly double minLight;

        public override int GrowthStage => Math.Min(8, this.AnimationFrame);
        public override int MaxGrowthStage => 8;
        public override bool CanFruitUnripe => true;
        public override bool HasFruitUnripe => this.AnimationFrame == 9;
        public override bool HasDeadFrame => true;
        public override bool IsDead => this.AnimationFrame == 12;

        public override int CountFruitAvailable
        {
            get
            {
                switch (this.AnimationFrame)
                {
                    case 10: return 2;
                    case 11: return 1;
                }

                return 0;
            }
        }

        public ShadowModel ShadowModel { get; protected set; } = new ShadowModel();

        public Bush() : base(ThingType.Bush)
        {
        }

        public Bush(ISmallTile mainTile, bool fromSeed = false) : base(ThingType.Bush, mainTile, 2)
        {
            if (fromSeed) this.AnimationFrame = 1;
            else
            {
                // From world generator.
                var r = Rand.Next(6);
                if (r == 0)
                {
                    this.Growth = 63 - Rand.Next(10);
                    this.AnimationFrame = 2;
                }
                else if (r == 1)
                {
                    this.Growth = 127 - Rand.Next(10);
                    this.AnimationFrame = 4;
                }
                else if (r == 2)
                {
                    this.Growth = 191 - Rand.Next(10);
                    this.AnimationFrame = 6;
                }
                else if (r >= 3)
                {
                    this.Growth = 255 - Rand.Next(10);
                    this.AnimationFrame = 8;
                }

                this.minTemp = 15 + Rand.Next(4);
                this.minLight = (Rand.NextDouble() * 0.25) + 0.1;
            }
        }

        public override void RemoveFruit(bool leaveSeed)
        {
            switch (this.AnimationFrame)
            {
                case 10: this.AnimationFrame = 11; World.HandleFruitPlantUpdate(this); break;
                case 11: this.AnimationFrame = 8; World.HandleFruitPlantUpdate(this); break;
            }

            base.RemoveFruit(leaveSeed);
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            if (this.AnimationFrame == 12)  // Dead
            {
                return new Dictionary<ItemType, int>
                {
                    { ItemType.Metal, 0 },
                    { ItemType.Biomass, 12 },
                    { ItemType.IronOre, 0 }
                };
            }

            var result = new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, Math.Min(20, 4 + (int)(this.AnimationFrame * 2.01f)) },   // 6 - 20 organics
                { ItemType.IronOre, 0 }
            };

            return result;
        }

        public override List<int> UpdateGrowth()
        {
            List<int> seedTiles = null;
            base.UpdateGrowth();

            // Lifecycle:
            // Growth happens when light and temperature above about 15C.
            // 1. Seeds germinate when light and temperature > 10C and hour < 100.  Frame 1.  Hour approx 80.  Growth = 0.
            // 2. Gets bigger after 32 hours.  Frame 2.  Hour approx 110.  Growth = 32.
            // 3. Gets bigger after 32 hours.  Frame 3.  Hour approx 80 day 2.  Growth = 64.
            // 4. Gets bigger after 32 hours.  Frame 4.  Hour approx 110 day 2.  Growth = 96.
            // 5. Gets bigger after 32 hours.  Frame 5.  Hour approx 80 day 3.  Growth = 128.
            // 6. Gets bigger after 32 hours.  Frame 6.  Hour approx 110 day 3.  Growth = 160.
            // 7. Gets bigger after 32 hours.  Frame 7.  Hour approx 80 day 4.  Growth = 192.
            // 8. Gets bigger after 32 hours.  Frame 8.  Hour approx 110 day 4.  Growth = 224.
            // 9. Green fruit after 32 hours.  Frame 9.  Hour approx 80 day 5.  Growth >= 256.  Happens 3 times.
            // 10. Yellow fruit after 32 hours.  Frame 10.  Hour approx 110 day 5.  Growth >= 288.  Happens 3 times.
            // 11. Fruit removed when cold and dark.
            // 12. Dies if cold and dark and growth > 416.  Frame 12.  Hour approx 180 day 7.
            // 12. When temperature drops below -5 to -10, plant is removed.  Early day 8.  Seed added in place.

            // Growth controlled by light and temperature triggers
            var light = RoomManager.GetTileLightLevel(this.MainTileIndex);
            var temperature = RoomManager.GetTileTemperature(this.MainTileIndex);

            if (light > this.minLight && temperature >= this.minTemp)
            {
                this.Growth++;
                if (this.Growth > 0 && this.Growth % 32 == 0 && this.AnimationFrame < 10)
                {
                    this.AnimationFrame++;
                }
            }
            else
            {
                if (this.AnimationFrame >= 8 && light <= minLight)
                {
                    if (temperature < this.minTemp && this.AnimationFrame < 12 && Rand.NextDouble() > 0.85)
                    {
                        if (this.Growth > 416)
                        {
                            // Brown leaves
                            this.AnimationFrame = 12;
                        }
                        else
                        {
                            // Remove fruit but drop seed
                            this.AnimationFrame = 8;
                            if (!this.Seeded)
                            {
                                var tile = this.allTiles[Rand.Next(this.allTiles.Count)].Index;
                                seedTiles = new List<int> { tile };
                            }
                        }
                    }
                    else if (temperature <= this.minTemp - 5 && Rand.NextDouble() > 0.85 && this.AnimationFrame == 12 && this.RecycleProgress == 0)
                    {
                        // Disappear in the cold and dark and leave a seed
                        World.RemoveThing(this);
                    }
                }
            }

            // Remove any fruit harvest jobs if the fruit have gone
            if (this.HarvestFruitPriority != WorkPriority.Disabled && this.CountFruitAvailable == 0)
            {
                this.HarvestFruitPriority = WorkPriority.Disabled;
                World.HandleFruitPlantUpdate(this);
            }

            if (this.seedNextFrame)
            {
                this.seedNextFrame = false;
                if (Rand.Next(2) == 0)
                {
                    var tile = this.allTiles[Rand.Next(this.allTiles.Count)].Index;
                    if (seedTiles == null) seedTiles = new List<int> { tile };
                }
            }

            return seedTiles;
        }
    }
}
