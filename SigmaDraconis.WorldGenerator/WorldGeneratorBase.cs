namespace SigmaDraconis.WorldGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Shared;

    public abstract class WorldGeneratorBase
    {
        public static bool IsLogEnabled { get; set; } = true;

        protected readonly WorldTemplate generatedTemplate = new WorldTemplate();
        protected readonly int size;
        private int landTileCount;

        public int StartTileIndex { get; set; }
        public bool IsReady { get; private set; }

        public WorldGeneratorBase(int mapSize)
        {
            this.size = mapSize;
        }

        public void BeginGenerate()
        {
            this.IsReady = false;
            Task.Run(Generate);
        }

        public void Generate()
        {
            var random = new Random();
            bool success = false;
            this.StartTileIndex = 0;

            while (!success)
            {
                Log("Generating world...");
                this.GenerateInner(random, size);
                Log("Creating landing zone...");
                this.StartTileIndex = this.FindLandingZone();
                if (this.StartTileIndex == 0)
                {
                    Log("Failed to find good starting position.  Clearing...");
                }
                else success = true;
            }

            Log("Finished generating world.");

            this.IsReady = true;
        }

        public void Create()
        {
            WorldCreator.Create(this.generatedTemplate, this.StartTileIndex);
            this.generatedTemplate.Clear();
        }

        private void GenerateInner(Random random, int size)
        {
            this.generatedTemplate.Clear(size);
            this.GenerateTerrain(random, size);

            this.landTileCount = 0;
            foreach (var tile in this.generatedTemplate.BigTiles)
            {
                tile.UpdateSmallTileTerrainTypes();
                if (tile.TerrainType == TerrainType.Dirt) landTileCount++;
            }

            // Add ore, coal etc.
            var resourcesByTile = new Dictionary<int, Tuple<ItemType, int>>();
            this.AddOre(resourcesByTile, random);
            this.FinalizeOre(resourcesByTile, random);
            this.AddRocks(random);
            this.AddBiomes(random);
            this.AddGroundCover();
            this.AddPlants(random);
        }

        protected virtual void GenerateTerrain(Random random, int size)
        {
            throw new NotImplementedException();
        }

        protected static bool CanTileBeLand(BigTileTemplate tile)
        {
            // Must have three adjacent land tiles, else coastal geometry doesn't work
            if (tile.TileToN?.TerrainType == TerrainType.Dirt && tile.TileToNE?.TerrainType == TerrainType.Dirt && tile.TileToE?.TerrainType == TerrainType.Dirt && tile.TileToSE?.TerrainType == TerrainType.Dirt) return true;
            if (tile.TileToNE?.TerrainType == TerrainType.Dirt && tile.TileToE?.TerrainType == TerrainType.Dirt && tile.TileToSE?.TerrainType == TerrainType.Dirt) return true;
            if (tile.TileToE?.TerrainType == TerrainType.Dirt && tile.TileToSE?.TerrainType == TerrainType.Dirt && tile.TileToS?.TerrainType == TerrainType.Dirt && tile.TileToSW?.TerrainType == TerrainType.Dirt) return true;
            if (tile.TileToSE?.TerrainType == TerrainType.Dirt && tile.TileToS?.TerrainType == TerrainType.Dirt && tile.TileToSW?.TerrainType == TerrainType.Dirt) return true;
            if (tile.TileToS?.TerrainType == TerrainType.Dirt && tile.TileToSW?.TerrainType == TerrainType.Dirt && tile.TileToW?.TerrainType == TerrainType.Dirt && tile.TileToNW?.TerrainType == TerrainType.Dirt) return true;
            if (tile.TileToSW?.TerrainType == TerrainType.Dirt && tile.TileToW?.TerrainType == TerrainType.Dirt && tile.TileToNW?.TerrainType == TerrainType.Dirt) return true;
            if (tile.TileToW?.TerrainType == TerrainType.Dirt && tile.TileToNW?.TerrainType == TerrainType.Dirt && tile.TileToN?.TerrainType == TerrainType.Dirt && tile.TileToNE?.TerrainType == TerrainType.Dirt) return true;
            if (tile.TileToNW?.TerrainType == TerrainType.Dirt && tile.TileToN?.TerrainType == TerrainType.Dirt && tile.TileToNE?.TerrainType == TerrainType.Dirt) return true;

            return false;
        }

        protected static bool IsNullOrWater(TerrainType? terrainType)
        {
            return terrainType == null || terrainType == TerrainType.Water || terrainType == TerrainType.DeepWater || terrainType == TerrainType.DeepWaterEdge;
        }

        protected virtual void AddOre(Dictionary<int, Tuple<ItemType, int>> resourcesByTile, Random random)
        {
            throw new NotImplementedException();
        }

        protected virtual ItemType GetRandomResourceType(Random random)
        {
            var r = random.Next(20);
            if (r >= 16) return ItemType.Coal;
            if (r >= 9) return ItemType.IronOre;
            return ItemType.Stone;
        }

        protected void AddOre(Dictionary<int, Tuple<ItemType, int>> resourcesByTile, Random random, int blobs, int minSize, int maxSize, int density)
        {
            Log("Adding ore");

            var openNodes = new Queue<int>();
            var closedNodes = new HashSet<int>();
            var blobsToDump = blobs;
            var tileCount = this.generatedTemplate.SmallTiles.Count;
            while (blobsToDump > 0)
            {
                var tileIndex = random.Next(tileCount);
                if (resourcesByTile.ContainsKey(tileIndex) && resourcesByTile[tileIndex].Item2 >= density) continue;

                var count = minSize + random.Next(maxSize - minSize);
                if (resourcesByTile.ContainsKey(tileIndex))
                {
                    resourcesByTile[tileIndex] = new Tuple<ItemType, int>(resourcesByTile[tileIndex].Item1, count);
                }
                else
                {
                    var type = this.GetRandomResourceType(random);
                    resourcesByTile.Add(tileIndex, new Tuple<ItemType, int>(type, count));
                }

                openNodes.Enqueue(tileIndex);
                blobsToDump--;
            }

            while (openNodes.Any())
            {
                var i = openNodes.Dequeue();

                if (resourcesByTile[i].Item2 <= density)
                {
                    closedNodes.Add(i);
                    continue;
                }

                var t = this.generatedTemplate.GetSmallTile(i);
                var toMove = resourcesByTile[i].Item2 - density;
                var adj = t.AdjacentTiles8.Where(x => !closedNodes.Contains(x.Index)).ToList();
                while (toMove > 0 && adj.Count > 0)
                {
                    var a = adj[random.Next(adj.Count)];
                    resourcesByTile[i] = new Tuple<ItemType, int>(resourcesByTile[i].Item1, resourcesByTile[i].Item2 - 1);
                    if (!resourcesByTile.ContainsKey(a.Index)) resourcesByTile.Add(a.Index, new Tuple<ItemType, int>(resourcesByTile[i].Item1, 1));
                    else resourcesByTile[a.Index] = new Tuple<ItemType, int>(resourcesByTile[a.Index].Item1, resourcesByTile[a.Index].Item2 + 1);
                    toMove--;
                }

                foreach (var a in adj.Where(j => resourcesByTile.ContainsKey(j.Index))) openNodes.Enqueue(a.Index);
                closedNodes.Add(t.Index);
            }
        }

        protected void FinalizeOre(Dictionary<int, Tuple<ItemType, int>> resourcesByTile, Random random)
        {
            foreach (var kv in resourcesByTile)
            {
                var tile = this.generatedTemplate.GetSmallTile(kv.Key);
                if (tile.TerrainType != TerrainType.Dirt) continue;

                var count = kv.Value.Item2 - random.Next(3);
                if (count < 2) count = 2;

                tile.SetResources(kv.Value.Item1, count, GetResourceDensity(count));
            }

            // Routine to try and reduce hard edges between different ore types
            foreach (var kv in resourcesByTile)
            {
                var tile = this.generatedTemplate.GetSmallTile(kv.Key);
                if (tile.TerrainType != TerrainType.Dirt || tile.MineResourceCount == 0) continue;

                var resource = tile.GetResources();
                foreach (var adj in tile.AdjacentTiles4)
                {
                    var adjResource = adj.GetResources();
                    if (adjResource == null || adjResource.Density <= MineResourceDensity.Low || adjResource.Type == resource.Type) continue;

                    // Decrease border densities
                    resource.Count = random.Next(tile.MineResourceCount) + 1;
                    adjResource.Count = random.Next(adj.MineResourceCount) + 1;
                    if (random.Next(5) == 0)
                    {
                        // 20% chance to swap types
                        var swapType = adjResource.Type;
                        adjResource.Type = resource.Type;
                        resource.Type = swapType;
                    }

                    resource.Density = GetResourceDensity(tile.MineResourceCount);
                    adjResource.Density = GetResourceDensity(adj.MineResourceCount);
                    tile.SetResources(resource.Type, resource.Count, resource.Density);
                    adj.SetResources(adjResource.Type, adjResource.Count, adjResource.Density);
                }
            }
        }

        protected static MineResourceDensity GetResourceDensity(int count)
        {
            var density = MineResourceDensity.None;
            if (count > 16) density = MineResourceDensity.VeryHigh;
            else if (count > 12) density = MineResourceDensity.High;
            else if (count > 8) density = MineResourceDensity.Medium;
            else if (count > 4) density = MineResourceDensity.Low;
            else if (count > 0) density = MineResourceDensity.VeryLow;
            return density;
        }

        protected void AddRocks(Random random)
        {
            Log("Adding rocks");

            var closedTiles = new HashSet<int>();

            var tilesWithOre = this.generatedTemplate.SmallTiles.Values.Where(t => t.MineResourceDensity >= MineResourceDensity.Low).ToList();

            var rockDensity = this.landTileCount / 140;

            // Big rocks
            int rocksToPlace = rockDensity * 5;
            int triesRemaining = 10000;
            var tileCount = tilesWithOre.Count;
            while (rocksToPlace > 0 && triesRemaining > 0)
            {
                var tile = tilesWithOre[random.Next(tilesWithOre.Count)];
                if (tile.TerrainType != TerrainType.Dirt
                    || tile.TileToNE?.TerrainType != TerrainType.Dirt || tile.TileToE?.TerrainType != TerrainType.Dirt || tile.TileToSE?.TerrainType != TerrainType.Dirt) continue;
                if (closedTiles.Contains(tile.Index)) continue;
                if (tile.MineResourceType == ItemType.IronOre
                    || tile.TileToNE.MineResourceType == ItemType.IronOre
                    || tile.TileToE.MineResourceType == ItemType.IronOre
                    || tile.TileToNW.MineResourceType == ItemType.IronOre)
                {
                    this.generatedTemplate.AddRock(tile, ThingType.RockLarge, ItemType.IronOre);
                    closedTiles.Add(tile.Index);
                    rocksToPlace--;
                }
                else if (tile.MineResourceType == ItemType.Stone
                    || tile.TileToNE.MineResourceType == ItemType.Stone
                    || tile.TileToE.MineResourceType == ItemType.Stone
                    || tile.TileToNW.MineResourceType == ItemType.Stone)
                {
                    this.generatedTemplate.AddRock(tile, ThingType.RockLarge, ItemType.Stone);
                    closedTiles.Add(tile.Index);
                    rocksToPlace--;
                }
                else if (random.Next(2) == 1 && (tile.MineResourceType == ItemType.Coal
                    || tile.TileToNE.MineResourceType == ItemType.Coal
                    || tile.TileToE.MineResourceType == ItemType.Coal
                    || tile.TileToNW.MineResourceType == ItemType.Coal))
                {
                    this.generatedTemplate.AddRock(tile, ThingType.RockLarge, ItemType.Coal);
                    closedTiles.Add(tile.Index);
                    rocksToPlace--;
                }
            }

            // Small rocks
            rocksToPlace = rockDensity * 15;
            triesRemaining = 30000;
            while (rocksToPlace > 0 && triesRemaining > 0)
            {
                triesRemaining--;
                var tile = tilesWithOre[random.Next(tilesWithOre.Count)];
                if (tile.TerrainType != TerrainType.Dirt || tile.ThingsAll.Any()) continue;
                if (closedTiles.Contains(tile.Index)) continue;
                if (tile.MineResourceType == ItemType.IronOre)
                {
                    this.generatedTemplate.AddRock(tile, ThingType.RockSmall, ItemType.IronOre);
                    closedTiles.Add(tile.Index);
                    rocksToPlace--;
                }
                else if (tile.MineResourceType == ItemType.Stone)
                {
                    this.generatedTemplate.AddRock(tile, ThingType.RockSmall, ItemType.Stone);
                    closedTiles.Add(tile.Index);
                    rocksToPlace--;
                }
                else if (random.Next(2) == 1 && (tile.MineResourceType == ItemType.Coal))
                {
                    this.generatedTemplate.AddRock(tile, ThingType.RockSmall, ItemType.Coal);
                    closedTiles.Add(tile.Index);
                    rocksToPlace--;
                }
            }

            // Some addtional random rocks
            rocksToPlace = rockDensity * 4;
            triesRemaining = 10000;
            while (rocksToPlace > 0 && triesRemaining > 0)
            {
                triesRemaining--;
                var tile = this.generatedTemplate.GetSmallTile(random.Next(this.generatedTemplate.SmallTiles.Count));

                if (tile.TerrainType != TerrainType.Dirt || tile.TileToNE?.TerrainType != TerrainType.Dirt || tile.TileToE?.TerrainType != TerrainType.Dirt || tile.TileToSE?.TerrainType != TerrainType.Dirt) continue;
                if (tile.ThingsAll.Any() || tile.TileToNE.ThingsAll.Any() || tile.TileToE.ThingsAll.Any() || tile.TileToSE.ThingsAll.Any()) continue;

                var oreType = tile.MineResourceCount > 0 && tile.MineResourceType != ItemType.None ? tile.MineResourceType : ItemType.Stone;

                this.generatedTemplate.AddRock(tile, ThingType.RockLarge, oreType);
                rocksToPlace--;
            }

            rocksToPlace = rockDensity * 20;
            triesRemaining = 10000;
            while (rocksToPlace > 0 && triesRemaining > 0)
            {
                triesRemaining--;
                var tile = this.generatedTemplate.GetSmallTile(random.Next(this.generatedTemplate.SmallTiles.Count));

                if (tile.TerrainType != TerrainType.Dirt || tile.ThingsAll.Any()) continue;

                var oreType = tile.MineResourceCount > 0 && tile.MineResourceType != ItemType.None ? tile.MineResourceType : ItemType.Stone;

                this.generatedTemplate.AddRock(tile, ThingType.RockSmall, oreType);
                rocksToPlace--;
            }
        }

        protected virtual void AddBiomes(Random random)
        {
            throw new NotImplementedException();
        }

        protected virtual void AddGroundCover()
        {
            throw new NotImplementedException();
        }

        protected virtual void AddPlants(Random random)
        {
            throw new NotImplementedException();
        }

        protected bool IsAreaEmpty(int x1, int y1, int x2, int y2)
        {
            for (int x = x1; x <= x2; x++)
            {
                for (int y = y1; y <= y2; y++)
                {
                    if (!(this.generatedTemplate.GetSmallTile(x, y) is SmallTileTemplate tile) || tile.BigTile.TerrainType != TerrainType.Dirt || tile.ThingsAll.Any()) return false;
                }
            }

            return true;
        }

        protected virtual int FindLandingZone()
        {
            throw new NotImplementedException();
        }

        protected virtual int GetStartTileScore(SmallTileTemplate tile)
        {
            throw new NotImplementedException();
        }

        protected LandingZoneStats GetLandingZoneStats(SmallTileTemplate tile1, int maxDistance)
        {
            var stats = new LandingZoneStats();
            var connectedTiles = new HashSet<int> { tile1.Index };

            int distance = 0;
            while (distance < maxDistance)
            {
                distance++;
                foreach (var tileIndex in connectedTiles.ToList())
                {
                    var tile = this.generatedTemplate.GetSmallTile(tileIndex);
                    foreach (var t in tile.AdjacentTiles4)
                    {
                        if (t.TerrainType != TerrainType.Dirt) continue;
                        if (t != null && !connectedTiles.Contains(t.Index))
                        {
                            connectedTiles.Add(t.Index);
                            if (!t.ThingsAll.Any())
                            {
                                stats.Space++;
                            }
                            else if (distance < 5 && t.ThingsAll.Any(a => a is PlantTemplate))
                            {
                                return stats;  //  Start position invalid if too close to plants
                            }
                            else
                            {
                                foreach (var rock in t.ThingsPrimary.OfType<RockTemplate>())
                                {
                                    switch (rock.ResourceType)
                                    {
                                        case ItemType.Coal: stats.Coal += rock.ThingType == ThingType.RockLarge ? 10 : 2; break;
                                        case ItemType.IronOre: stats.Ore += rock.ThingType == ThingType.RockLarge ? 10 : 2; break;
                                        case ItemType.Stone: stats.Stone += rock.ThingType == ThingType.RockLarge ? 10 : 2; break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return stats;
        }

        protected static void Log(string message)
        {
            if (IsLogEnabled) Logger.Instance.Log("WorldGenerator", message);
        }

        protected internal class LandingZoneStats
        {
            public int Coal { get; set; }
            public int Ore { get; set; }
            public int Stone { get; set; }
            public int Space { get; set; }
        }
    }
}
