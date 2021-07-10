namespace SigmaDraconis.WorldControllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Config;
    using Shared;
    using World;
    using World.Flora;
    using World.Terrain;
    using WorldInterfaces;

    public static class PlantGrowthController
    {
        private static readonly FastRandom random = new FastRandom();

        // Each plant updates growth every hour
        private static readonly Queue<int> updateQueue = new Queue<int>();

        // Each plant updates growth every hour
        public static Queue<PlantSeed> SeedQueue = new Queue<PlantSeed>();

        private static bool isInitialised;
        private static bool isSubscribedToEvent;
        private static long lastUpdateFrame;

        public static void Clear()
        {
            updateQueue.Clear();
            SeedQueue.Clear();
            isInitialised = false;
        }

        public static void Init()
        {
            var currentFrame = World.WorldTime.FrameNumber;

            updateQueue.Clear();

            // Randomise first update frame in case it's zero
            foreach (var plant in World.GetThings<Plant>(ThingTypeManager.PlantThingTypes).Where(p => p.NextGrowthUpdateFrame == 0))
            {
                var nextFrame = currentFrame + (long)(random.NextFloat() * 3600);
                plant.NextGrowthUpdateFrame = nextFrame;
            }

            // Create the queue in the right order
            foreach (var plant in World.GetThings<Plant>(ThingTypeManager.PlantThingTypes).OrderBy(p => p.NextGrowthUpdateFrame))
            {
                updateQueue.Enqueue(plant.Id);
            }

            if (World.InitialEmptyTileFraction == 0)
            {
                // This is used by plant growth algorithm
                var emptyTileCount1 = World.SmallTiles.Count(t => t.TerrainType == TerrainType.Dirt && t.BiomeType == BiomeType.SmallPlants && t.ThingsAll.Count == 0);
                var emptyTileCount2 = World.SmallTiles.Count(t => t.TerrainType == TerrainType.Dirt && t.BiomeType != BiomeType.SmallPlants && t.BiomeType != BiomeType.Desert && t.ThingsAll.Count == 0);
                World.InitialEmptyTileFraction = (emptyTileCount1 + (emptyTileCount2 / 25)) / (float)World.SmallTiles.Count;
            }

            if (!isSubscribedToEvent)
            {
                isSubscribedToEvent = true;
                EventManager.Subscribe(EventType.Plant, EventSubType.Added, delegate (object obj) { OnPlantAdded(obj as IPlant); });
            }

            isInitialised = true;
        }

        public static void Update()
        {
            var currentFrame = World.WorldTime.FrameNumber;
            if (!isInitialised || currentFrame != lastUpdateFrame + 1) Init();
            lastUpdateFrame = currentFrame;

            var random = new Random();

            if (SeedQueue.Any())
            {
                var seed = SeedQueue.Peek();
                while (SeedQueue.Any() && (seed == null || seed.NextUpdateFrame <= currentFrame))
                {
                    SeedQueue.Dequeue();
                    var tile = World.GetSmallTile(seed.TileIndex);
                    if (tile.TerrainType == TerrainType.Dirt)
                    {
                        if (seed.ThingType == ThingType.SmallPlant1 && World.WorldTime.Hour < 92 && World.Temperature > 0 + random.Next(10) && !tile.ThingsAll.Any())
                        {
                            var plant = new SmallPlant1(tile, 0);
                            AddPlantToWorld(plant);
                        }
                        else if (seed.ThingType == ThingType.SmallPlant2 && World.WorldTime.Hour < 100 && World.Temperature > 5 + random.Next(10) && !tile.ThingsAll.Any())
                        {
                            var plant = new SmallPlant2(tile);
                            AddPlantToWorld(plant);
                        }
                        else if (seed.ThingType == ThingType.SmallPlant3 && World.WorldTime.Hour < 100 && World.Temperature > 0 + random.Next(10) && !tile.ThingsAll.Any())
                        {
                            var plant = new SmallPlant3(tile, true);
                            AddPlantToWorld(plant);
                        }
                        else if (seed.ThingType == ThingType.SmallPlant4 && World.WorldTime.Hour < 100 && World.Temperature > 0 + random.Next(10) && !tile.ThingsAll.Any())
                        {
                            var plant = new SmallPlant4(tile, true);
                            AddPlantToWorld(plant);
                        }
                        else if (seed.ThingType == ThingType.SmallPlant5 && World.WorldTime.Hour < 75 && World.Temperature > 0 + random.Next(5) && !tile.ThingsAll.Any())
                        {
                            var plant = new SmallPlant5(tile, true);
                            AddPlantToWorld(plant);
                        }
                        else if (seed.ThingType == ThingType.SmallPlant6 && World.WorldTime.Hour < 90 && World.Temperature > 5 + random.Next(10) && !tile.ThingsAll.Any())
                        {
                            var plant = new SmallPlant6(tile, true);
                            AddPlantToWorld(plant);
                        }
                        else if (seed.ThingType == ThingType.SmallPlant8 && World.WorldTime.Hour < 100 && World.Temperature > 0 + random.Next(10) && !tile.ThingsAll.Any())
                        {
                            var plant = new SmallPlant8(tile, true);
                            AddPlantToWorld(plant);
                        }
                        else if (seed.ThingType == ThingType.SmallPlant9 && World.WorldTime.Hour < 100 && World.Temperature > 0 + random.Next(10) && !tile.ThingsAll.Any())
                        {
                            var plant = new SmallPlant9(tile, true);
                            AddPlantToWorld(plant);
                        }
                        else if (seed.ThingType == ThingType.SmallPlant12 && World.WorldTime.Hour < 100 && World.Temperature > 0 + random.Next(10) && !tile.ThingsAll.Any())
                        {
                            var plant = new SmallPlant12(tile, true);
                            AddPlantToWorld(plant);
                        }
                        else if (seed.ThingType == ThingType.Bush && World.WorldTime.Hour < 100 && World.Temperature >= 15 + random.Next(10) && IsAreaEmpty(tile.X, tile.Y, tile.X + 1, tile.Y + 1))
                        {
                            var plant = new Bush(tile, true);
                            AddPlantToWorld(plant);
                        }
                        else if (seed.GrowthAttemptCount < WorldTime.HoursInDay * 2)
                        {
                            seed.NextUpdateFrame = currentFrame + (int)Constants.FramesPerHour;
                            seed.GrowthAttemptCount++;
                            SeedQueue.Enqueue(seed);
                        }
                    }

                    seed = SeedQueue.Any() ? SeedQueue.Peek() : null;
                }
            }

            if (!updateQueue.Any()) return;

            var next = World.GetThing(updateQueue.Peek()) as IPlant;
            while (updateQueue.Any() && (next == null || next.NextGrowthUpdateFrame <= currentFrame))
            {
                updateQueue.Dequeue();
                if (next != null)
                {
                    // Update and put to back of queue
                    var seedTiles = next.UpdateGrowth();
                    if (seedTiles != null)
                    {
                        foreach (var seedTile in seedTiles) AddSeed(next.ThingType, seedTile);
                    }

                    next.NextGrowthUpdateFrame = currentFrame + 3600;
                    updateQueue.Enqueue(next.Id);
                }

                next = updateQueue.Any() ? World.GetThing(updateQueue.Peek()) as IPlant : null;
            }
        }

        private static void AddPlantToWorld(IPlant plant)
        {
            plant.NextGrowthUpdateFrame = World.WorldTime.FrameNumber + (int)Constants.FramesPerHour;
            World.AddThing(plant);
            updateQueue.Enqueue(plant.Id);
        }

        private static void OnPlantAdded(IPlant plant)
        {
            if (plant.NextGrowthUpdateFrame > 0) return;   // Already added
            plant.NextGrowthUpdateFrame = World.WorldTime.FrameNumber + (int)Constants.FramesPerHour;
            updateQueue.Enqueue(plant.Id);
        }

        public static void AddSeed(ThingType thingType, int tileIndex, int nextUpdateOffset = (int)Constants.FramesPerHour)
        {
            var currentFrame = World.WorldTime.FrameNumber;
            var seed = new PlantSeed { NextUpdateFrame = currentFrame + nextUpdateOffset, ThingType = thingType, TileIndex = tileIndex };
            SeedQueue.Enqueue(seed);
        }

        /// <summary>
        /// Call after world built, to ensure seeds are in order of next update
        /// </summary>
        public static void SortSeedQueue()
        {
            var sorted = SeedQueue.OrderBy(s => s.NextUpdateFrame).ToList();
            SeedQueue.Clear();
            foreach (var seed in sorted) SeedQueue.Enqueue(seed);
        }

        private static bool IsAreaEmpty(int x1, int y1, int x2, int y2)
        {
            for (int x = x1; x <= x2; x++)
            {
                for (int y = y1; y <= y2; y++)
                {
                    if (!(World.GetSmallTile(x, y) is ISmallTile tile) || tile.BigTile.TerrainType != TerrainType.Dirt || tile.ThingsAll.Any()) return false;
                }
            }

            return true;
        }
    }
}
