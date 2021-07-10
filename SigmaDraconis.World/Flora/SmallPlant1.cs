namespace SigmaDraconis.World.Flora
{
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    /// <summary>
    /// Whiteflower
    /// </summary>
    [ProtoContract]
    public class SmallPlant1 : Plant, IPlantWithAnimatedFlower, IAnimatedThing
    {
        private static readonly int[] seedAnimationSequence = new int[12] { 17, 18, 19, 20, 21, 22, 23, 24, 23, 22, 21, 22 };

        [ProtoMember(1)]
        public int SeedOpeningAnimationStep { get; set; }

        public int? FlowerRenderLayer => (this.AnimationFrame > 6 && this.AnimationFrame < 25) ? 2 : (int?)null;
        public int? FlowerFrame => (this.AnimationFrame > 7 && this.AnimationFrame < 25) ? this.AnimationFrame - 7 : 1;

        public override int GrowthStage => this.animationFrame <= 16 ? (this.animationFrame + 3) / 4 : 5;
        public override int MaxGrowthStage => 5;
        public override bool CanFlower => true;
        public override bool IsFlowering => this.AnimationFrame > 16 && this.AnimationFrame < 25;
        public override bool IsDead => this.AnimationFrame == 25;

        public SmallPlant1() : base(ThingType.SmallPlant1)
        {
        }

        public SmallPlant1(ISmallTile tile, float age) : base(ThingType.SmallPlant1, tile, 1)
        {
            this.age = age;
        }

        public override void AfterAddedToWorld()
        {
            this.UpdateAnimationFrame();
            base.AfterAddedToWorld();
        }

        public override void Update()
        {
            if (this.AnimationFrame >= 17 && this.AnimationFrame < 25 && this.SeedOpeningAnimationStep <= 33)
            {
                var sequenceStep = this.SeedOpeningAnimationStep / 3;
                this.AnimationFrame = seedAnimationSequence[sequenceStep];
                this.SeedOpeningAnimationStep++;
                EventManager.RaiseEvent(EventType.Plant, EventSubType.Updated, this);
            }
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var biomass = 1;
            if (this.AnimationFrame == 25) biomass = 2;
            else if (this.AnimationFrame >= 12) biomass = 4;
            else if (this.AnimationFrame >= 8) biomass = 3;
            else if (this.AnimationFrame >= 4) biomass = 2;

            var result = new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, biomass },
                { ItemType.IronOre, 0 }
            };

            return result;
        }

        public override string GetTextureName(int layer = 1)
        {
            return layer == 1 ? base.GetTextureName() : $"SmallPlant1Flower_{this.FlowerFrame}";
        }

        public override List<int> UpdateGrowth()
        {
            List<int> seedTiles = null;
            base.UpdateGrowth();
            this.UpdateAnimationFrame();
            if (this.AnimationFrame == 17) seedTiles = this.SpreadSeed();
            var hour = (World.WorldTime.TotalHoursPassed % WorldTime.HoursInDay) + 1;
            if (hour >= 150 && World.Temperature < 0 && this.AnimationFrame == 24 && this.RecycleProgress == 0 && (Rand.NextDouble() > 0.85 || hour > 170))
            {
                // Dies at night
                this.AnimationFrame = 25;
            }

            if (hour >= 170 && this.AnimationFrame >= 24 && this.RecycleProgress == 0 && (Rand.NextDouble() > 0.85 || hour > 180))
            {
                World.RemoveThing(this);
            }

            return seedTiles;
        }

        private List<int> SpreadSeed()
        {
            // To keep a roughly constant population, number of seeds is 1 / EmptyTileFraction
            var seedCount = 1f / World.InitialEmptyTileFraction;

            // This won't be a round nnumber, so the last seed has a success probability...
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

            return seedTiles;
        }

        private void UpdateAnimationFrame()
        {
            if (this.AnimationFrame == 25) return;

            // Starts growing at 0C, approx. hour 63
            // Seeds after 68 hours
            // Dies at hour 190
            if (this.AnimationFrame < 17) this.AnimationFrame = (int)((this.age + 4) / 4);
            else this.AnimationFrame = 24;
        }
    }
}
