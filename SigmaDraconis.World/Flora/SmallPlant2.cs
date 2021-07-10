namespace SigmaDraconis.World.Flora
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    /// <summary>
    /// Blue Lantern plant
    /// </summary>
    [ProtoContract]
    public class SmallPlant2 : Plant, IAnimatedThing
    {
        [ProtoMember(1)]
        private bool isSeeded;

        public override int GrowthStage => this.AnimationFrame;
        public override int MaxGrowthStage => 12;
        public override bool CanFlower => true;
        public override bool IsFlowering => this.AnimationFrame == 12;

        public SmallPlant2() : base(ThingType.SmallPlant2)
        {
        }

        public SmallPlant2(ISmallTile tile) : base(ThingType.SmallPlant2, tile, 1)
        {
        }

        public override void AfterAddedToWorld()
        {
            this.UpdateAnimationFrame();
            base.AfterAddedToWorld();
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var biomass = 1;
            if (this.AnimationFrame >= 10) biomass = 3;
            else if (this.AnimationFrame >= 6) biomass = 2;

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
            // 1. Grows all the time temp is +ve and light > 90%, 1% per hour up to 66%, then 0.8% per hour
            if (World.Temperature > 5 && World.WorldLight.NightLightFactor < 0.1f)
            {
                this.GrowthPercent = Math.Min(100.0, this.GrowthPercent + (this.GrowthPercent < 67.0 ? 1.0 : 0.8));
            }

            // 2. Seeds if growth is 100% and dark
            List<int> seedTiles = null;
            if (this.GrowthPercent > 99.0 && !this.isSeeded && World.WorldLight.NightLightFactor > .99f) seedTiles = this.SpreadSeed();

            // 3. Dies if growth is 100% and temp < -20
            var minTemp = World.ClimateType == ClimateType.Severe ? -20 : -7;
            if (World.Temperature < -7 && this.GrowthPercent > 99.0 && this.RecycleProgress == 0 && (Rand.NextDouble() > 0.85 || World.Temperature < minTemp))
            {
                World.RemoveThing(this);
            }
            else
            {
                this.UpdateAnimationFrame();
            }

            EventManager.RaiseEvent(EventType.Plant, EventSubType.Updated, this);

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
            this.AnimationFrame = this.GrowthPercent > 99.0 ? 12 : (int)(this.GrowthPercent * 0.11) + 1;
        }
    }
}
