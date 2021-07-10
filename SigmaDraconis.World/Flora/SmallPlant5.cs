namespace SigmaDraconis.World.Flora
{
    using System;
    using System.Collections.Generic;
    using ProtoBuf;
    using Shared;
    using Rooms;
    using WorldInterfaces;

    /// <summary>
    /// Bluefruit
    /// </summary>
    [ProtoContract]
    public class SmallPlant5 : FruitPlant, IAnimatedThing
    {
        [ProtoMember(1)]
        public int Pollinated { get; private set; }

        [ProtoMember(2)]
        public int Growth { get; private set; }

        [ProtoMember(3)]
        public int FruitGrowth { get; private set; }

        [ProtoMember(4)]
        private readonly int temperatureRequirementOffset;

        [ProtoMember(5)]
        private readonly double minLight;

        public override bool CanFruit => !this.IsDead && this.FruitGrowth < 35;
        public override int GrowthStage => Math.Min(3, (this.AnimationFrame + 7) / 8) + (this.AnimationFrame > 32 ? 1 : 0);
        public override int MaxGrowthStage => 4;
        public override bool CanFlower => true;
        public override bool IsFlowering => this.animationFrame >= 25 && this.animationFrame <= 32;

        public override int CountFruitAvailable
        {
            get
            {
                return (this.AnimationFrame > 32 && this.AnimationFrame <= 40) ? 1 : 0;
            }
        }

        public SmallPlant5() : base(ThingType.SmallPlant5)
        {
        }

        public SmallPlant5(ISmallTile tile, bool fromSeed = false) : base(ThingType.SmallPlant5, tile, 1)
        {
            if (fromSeed) this.AnimationFrame = Rand.Next(8) + 1;
            else
            {
                // From world generator.
                this.Growth = 30;
                this.FruitGrowth = 10;
                this.Pollinated = 1;
                this.AnimationFrame = Rand.Next(8) + 33;
            }

            this.temperatureRequirementOffset = Rand.Next(4);
            this.minLight = (Rand.NextDouble() * 0.25) + 0.1;
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var biomass = 1;
            if (this.AnimationFrame >= 17) biomass = 3;
            else if (this.AnimationFrame >= 9) biomass = 2;

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
            if (this.AnimationFrame > 32 && this.AnimationFrame <= 40)
            {
                this.AnimationFrame += 8;
                this.FruitGrowth = 35;
                World.HandleFruitPlantUpdate(this);
            }

            base.RemoveFruit(leaveSeed);
        }

        // This is called every hour
        public override List<int> UpdateGrowth()
        {
            List<int> seedTiles = null;
            base.UpdateGrowth();

            // Lifecycle:
            // 1. Seeds germinate when light but still cold.  Small plant frames 1 - 8.
            // 2. Once warm, grows for 10 hours then gets bigger.  Frames 9 - 16.
            // 3. Grows for another 20 hours then gets bigger again.  Frames 17 - 24.
            // 4. After another 10 hours begins flowering (should be around hour 120).  Frames 25 - 32.
            // 5. When getting dark, stops flowering.  Frames 17 - 24.
            // 6. Once completely dark, leaves turn partially brown.  Frames 41 - 48.
            // 7. Once light and temperature > ~-6, fruit appear.  Frames 33 - 40.
            // 8. After 30 hours with temperature > ~0, fruit removed.  Frames 41 - 48.
            // 9. When temperature > ~25, leaves turn completely brown.  Frames 49 - 56.
            // 10. When dark, leaves start falling off.  Fames 57 - 64.
            // 11. When temperature drops below zero, plant is removed.  Seed added so the plant will grow again next year.

            // Growth controlled by light and temperature triggers
            var light = RoomManager.GetTileLightLevel(this.MainTileIndex);
            var temperature = RoomManager.GetTileTemperature(this.MainTileIndex);

            if (light > this.minLight)
            {
                if (temperature >= this.temperatureRequirementOffset && this.Growth < 40)
                {
                    // Grow
                    this.Growth++;
                    if (this.AnimationFrame <= 16 && this.Growth >= 30) this.AnimationFrame += 8;
                    else if (this.AnimationFrame <= 8 && this.Growth >= 10) this.AnimationFrame += 8;
                }
                else if (temperature >= this.temperatureRequirementOffset + 10 && this.AnimationFrame > 16 && this.AnimationFrame <= 24 && this.Pollinated == 0)
                {
                    // Flower (frames 25 - 32)
                    this.AnimationFrame += 8;
                    this.Pollinated = 1;
                }
                else if (this.AnimationFrame > 40 && this.AnimationFrame <= 48 && this.FruitGrowth > 0 && temperature >= this.temperatureRequirementOffset - 6)
                {
                    if (this.FruitGrowth > 10 && this.FruitGrowth < 35)
                    {
                        // Fruit (frames 33 - 40)
                        this.AnimationFrame -= 8;
                    }
                    else this.FruitGrowth++;
                }
                else if (this.AnimationFrame > 32 && this.AnimationFrame <= 40 && this.FruitGrowth > 0 && temperature >= this.temperatureRequirementOffset)
                {
                    this.FruitGrowth++;
                    if (this.FruitGrowth >= 35)
                    {
                        // Remove fruit
                        this.AnimationFrame += 8;
                        seedTiles = new List<int> { this.MainTileIndex };
                        if (Rand.Next(8) == 0)
                        {
                            // Small chance to also spread to an adjacent tile
                            var r = Rand.Next(8);
                            var tile = this.MainTile.AdjacentTiles8[r];
                            if (tile != null) seedTiles.Add(tile.Index);
                        }
                    }
                }
                else if (this.FruitGrowth >= 35 && this.AnimationFrame > 40 && this.AnimationFrame <= 48 && World.Temperature > this.temperatureRequirementOffset + 25)
                {
                    // Leaves turn brown in the heat (frames 49 - 56)
                    this.AnimationFrame += 8;
                }
            }
            else
            {
                if (this.AnimationFrame > 24 && this.AnimationFrame <= 32)
                {
                    // Stop flowering if dark
                    this.AnimationFrame -= 8;
                    this.FruitGrowth = 1;
                }
                else if (this.AnimationFrame > 16 && this.AnimationFrame <= 24 && this.FruitGrowth > 0 && light < 0.05)
                {
                    // Leaves turn semi-brown (frames 41 - 48)
                    this.AnimationFrame += 24;
                }
                else if (this.AnimationFrame > 40 && this.AnimationFrame <= 48 && this.FruitGrowth < 10 && temperature > 0)
                {
                    // Fruit grow only when warm and dark (but don't appear until it gets light again)
                    this.FruitGrowth++;
                }
                else if (this.AnimationFrame > 48 && this.AnimationFrame <= 56)
                {
                    // Leaves dropping off (frames 57 - 64)
                    this.AnimationFrame += 8;
                }
                else if (World.Temperature < 0 && this.FruitGrowth >= 35 && this.RecycleProgress == 0 && (Rand.NextDouble() > 0.85 || World.Temperature < -15))
                { 
                    // Disappear in the cold and dark and leave a seed
                    World.RemoveThing(this);
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
