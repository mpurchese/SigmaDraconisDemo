namespace SigmaDraconis.World.Flora
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    /// <summary>
    /// Firebrush Plant
    /// </summary>
    [ProtoContract]
    public class SmallPlant3 : Plant, IAnimatedThing
    {
        [ProtoMember(1)]
        private bool isSeeded;

        [ProtoMember(2)]
        private int growthPhase;

        [ProtoMember(3)]
        private float closedFaction;

        [ProtoMember(4)]
        private readonly int openTemp;

        [ProtoMember(5)]
        private readonly int dieTemp;

        [ProtoMember(6)]
        private int hoursMature;

        [ProtoMember(7)]
        private readonly int maxHoursMature;

        public override int GrowthStage => Math.Min(8, (this.AnimationFrame + 1) / 2);
        public override int MaxGrowthStage => 8;
        public override bool CanFlower => true;
        public override bool IsFlowering => this.growthPhase >= 3 && this.closedFaction < 1f;
        public override bool IsDead => this.AnimationFrame == 49;

        public SmallPlant3() : base(ThingType.SmallPlant3)
        {
        }

        public SmallPlant3(ISmallTile tile, bool fromSeed) : base(ThingType.SmallPlant3, tile, 1)
        {
            if (!fromSeed)
            {
                // Closed for night
                this.growthPhase = 3;
                this.closedFaction = 1f;
                this.GrowthPercent = 100f;
            }
            else
            {
                this.growthPhase = 1;
                this.age = 0;
            }

            this.openTemp = Rand.Next(5);
            this.dieTemp = (World.ClimateType == ClimateType.Severe ? -10 : -2) - Rand.Next(5);
            this.maxHoursMature = 80 + Rand.Next(40);
        }

        public override void AfterAddedToWorld()
        {
            this.UpdateAnimationFrame();
            base.AfterAddedToWorld();
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            int biomass;
            if (this.AnimationFrame == 49) biomass = 2;
            else if (this.growthPhase > 1) biomass = 4;
            else biomass = 1 + (int)(this.GrowthPercent / 35f);

            var result = new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, biomass },
                { ItemType.IronOre, 0 }
            };

            return result;
        }

        public override List<int> UpdateGrowth()
        {
            base.UpdateGrowth();

            // Rules:
            // 1. If growing, then grows all the time temp is +ve and light > 90%
            if (this.growthPhase == 1 && World.WorldLight.NightLightFactor < 0.1f && World.Temperature > this.openTemp)
            {
                this.GrowthPercent += 1.6f;
                if (this.GrowthPercent >= 100f)
                {
                    this.GrowthPercent = 100f;
                    this.growthPhase = 2;
                }
            }

            // 2. If finished growing, then close when dark
            if (this.growthPhase == 2 && World.WorldLight.NightLightFactor > 0.9f)
            {
                this.closedFaction += 1 / 16f;
                if (this.closedFaction >= 1f)
                {
                    this.closedFaction = 1f;
                    this.growthPhase = 3;
                }
            }

            // 3. If closed, then open when warm and light
            if (this.growthPhase == 3 && World.WorldLight.NightLightFactor < 0.1f && World.Temperature > this.openTemp)
            {
                this.closedFaction -= 1 / 16f;
                if (this.closedFaction <= 0f)
                {
                    this.closedFaction = 0f;
                    this.growthPhase = 4;
                }
            }

            // 4. Seeds after dark if mature
            List<int> seedTiles = null;
            if (this.growthPhase == 4 && !this.isSeeded && World.WorldLight.NightLightFactor > .99f)
            {
                seedTiles = this.SpreadSeed();
            }

            // 5. Eventually dies if mature, or if not fully grown and very cold
            if (this.growthPhase == 4 && this.AnimationFrame >= 48)
            {
                this.hoursMature++;
                if ((this.hoursMature > this.maxHoursMature || World.Temperature < this.dieTemp) && this.RecycleProgress == 0)
                {
                    World.RemoveThing(this);
                }
                else if ((this.hoursMature > this.maxHoursMature - 4 || World.Temperature < this.dieTemp + 3) && this.RecycleProgress == 0)
                {
                    this.AnimationFrame = 49;   // Dead
                }
            }
            else if (this.growthPhase == 1 && World.Temperature < this.dieTemp && this.RecycleProgress == 0)
            {
                World.RemoveThing(this);
            }

            this.UpdateAnimationFrame();
            return seedTiles;
        }

        private List<int> SpreadSeed()
        {
            // To keep a roughly constant population, number of seeds is 1 / EmptyTileFraction
            var seedCount = 1f / World.InitialEmptyTileFraction;

            // This won't be a round number, so the last seed has a success probability...
            var intSeedCount = (int)seedCount;
            var dec = seedCount - intSeedCount;

            var tileIndexes = new List<int>();
            var worldTileCount = World.SmallTiles.Count;
            for (int i = 0; i < intSeedCount; i++) tileIndexes.AddIfNew(Rand.Next(worldTileCount));
            if (Rand.NextDouble() < dec) tileIndexes.AddIfNew(Rand.Next(worldTileCount));

            var seedTiles = new List<int>();
            foreach (var tile in tileIndexes.Select(i => World.GetSmallTile(i)).Where(t => t.TerrainType == TerrainType.Dirt && t.ThingsAll.Count == 0 && (t.BiomeType == BiomeType.SmallPlants || (t.BiomeType != BiomeType.Desert && Rand.Next(25) == 0))))
            {
                seedTiles.Add(tile.Index);
            }

            this.isSeeded = true;
            return seedTiles;
        }

        private void UpdateAnimationFrame()
        {
            if (this.AnimationFrame == 49) return;

            if (this.growthPhase == 1)
            {
                this.AnimationFrame = 1 + Math.Min(15, (int)(this.GrowthPercent * 16f / 100f));
            }
            else if (this.growthPhase == 2)
            {
                this.AnimationFrame = 16 + (int)((this.closedFaction + 0.01f) * 16f);
            }
            else if (this.growthPhase == 3)
            {
                this.AnimationFrame = 48 - (int)((this.closedFaction + 0.01f) * 16f);
            }
            else if (this.growthPhase == 4)
            {
                this.AnimationFrame = 48;
            }
        }
    }
}
