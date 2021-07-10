namespace SigmaDraconis.WorldGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Shared;
    using World.Flora;
    using World.Terrain;
    using WorldControllers;

    public class WorldGeneratorNormal : WorldGeneratorBase
    {
        public WorldGeneratorNormal(int mapSize = 64) : base(mapSize)
        {
        }

        protected override void GenerateTerrain(Random random, int size)
        {
            var lakeFreq = 4 + random.Next(4);
            var pondFreq = 8 + random.Next(4);
            var islandFreq = 4 + random.Next(2);
            Log($"Generating terrain.  LakeFreq = {lakeFreq}, islandFreq = {islandFreq}");

            var heightGrid = new float[size + 1, size + 1];
            for (int x = 0; x <= size; ++x)
            {
                for (int y = 0; y <= size; ++y)
                {
                    heightGrid[x, y] = 0f;
                }
            }

            heightGrid[size / 2, size / 2] = 1.6f;

            for (var scale = size / 4; scale >= 1; scale /= 2)
            {
                for (var x = scale; x <= size; x += scale * 2)
                {
                    for (var y = scale; y <= size; y += scale * 2)
                    {
                        var p1 = heightGrid[x - scale, y - scale];
                        var p2 = heightGrid[x + scale, y - scale];
                        var p3 = heightGrid[x - scale, y + scale];
                        var p4 = heightGrid[x + scale, y + scale];
                        heightGrid[x, y] = (0.25f * (p1 + p2 + p3 + p4)) + (scale * 0.1f * ((float)random.NextDouble() - 0.5f));
                    }
                }

                for (var x = scale; x <= size; x += scale * 2)
                {
                    for (var y = scale * 2; y <= size - (scale * 2); y += scale * 2)
                    {
                        var p1 = heightGrid[x - scale, y];
                        var p2 = heightGrid[x, y - scale];
                        var p3 = heightGrid[x, y + scale];
                        var p4 = heightGrid[x + scale, y];
                        heightGrid[x, y] = (0.25f * (p1 + p2 + p3 + p4)) + (scale * 0.1f * ((float)random.NextDouble() - 0.5f));
                    }
                }

                for (var x = scale * 2; x <= size - (scale * 2); x += scale * 2)
                {
                    for (var y = scale; y <= size; y += scale * 2)
                    {
                        var p1 = heightGrid[x - scale, y];
                        var p2 = heightGrid[x, y - scale];
                        var p3 = heightGrid[x, y + scale];
                        var p4 = heightGrid[x + scale, y];
                        heightGrid[x, y] = (0.25f * (p1 + p2 + p3 + p4)) + (scale * 0.1f * ((float)random.NextDouble() - 0.5f));
                    }
                }
            }

            // Flatten
            for (int x = 0; x <= size; ++x)
            {
                for (int y = 0; y <= size; ++y)
                {
                    if (heightGrid[x, y] > 1f)
                    {
                        heightGrid[x, y] = 1f;
                    }
                }
            }

            // Add lakes
            for (int i = 0; i < lakeFreq; i++)
            {
                var x = Rand.Next(size);
                var y = Rand.Next(size);
                if (heightGrid[x, y] < 0.5f) continue;

                var lakeDepth = Rand.NextFloat() * 2f;
                var lakeGradient = 0.75f + (Rand.NextFloat() * 0.25f);
                var regularity = Rand.Next(5) + 5;

                heightGrid[x, y] -= lakeDepth;

                var openNodes = new List<Vector2i> { new Vector2i(x, y) };
                var closedNodes = new HashSet<int> { (y * size) + x };
                while (openNodes.Any())
                {
                    var copy = openNodes.ToList();
                    openNodes.Clear();
                    foreach (var node in copy)
                    {
                        var h = heightGrid[node.X, node.Y];

                        if (node.X > 0 && !closedNodes.Contains((node.Y * size) + node.X - 1) && heightGrid[node.X - 1, node.Y] > h)
                        {
                            heightGrid[node.X - 1, node.Y] = h + (Rand.Next(10) < regularity ? Rand.NextFloat() * lakeGradient : 0);
                            openNodes.Add(new Vector2i(node.X - 1, node.Y));
                            closedNodes.Add((node.Y * size) + node.X - 1);
                        }

                        if (node.X < size - 1 && !closedNodes.Contains((node.Y * size) + node.X + 1) && heightGrid[node.X + 1, node.Y] > h)
                        {
                            heightGrid[node.X + 1, node.Y] = h + (Rand.Next(10) < regularity ? Rand.NextFloat() * lakeGradient : 0);
                            openNodes.Add(new Vector2i(node.X + 1, node.Y));
                            closedNodes.Add((node.Y * size) + node.X + 1);
                        }

                        if (node.Y > 0 && !closedNodes.Contains(((node.Y - 1) * size) + node.X) && heightGrid[node.X, node.Y - 1] > h)
                        {
                            heightGrid[node.X, node.Y - 1] = h + (Rand.Next(10) < regularity ? Rand.NextFloat() * lakeGradient : 0);
                            openNodes.Add(new Vector2i(node.X, node.Y - 1));
                            closedNodes.Add(((node.Y - 1) * size) + node.X);
                        }

                        if (node.Y < size - 1 && !closedNodes.Contains(((node.Y + 1) * size) + node.X) && heightGrid[node.X, node.Y + 1] > h)
                        {
                            heightGrid[node.X, node.Y + 1] = h + (Rand.Next(10) < regularity ? Rand.NextFloat() * lakeGradient : 0);
                            openNodes.Add(new Vector2i(node.X, node.Y + 1));
                            closedNodes.Add(((node.Y + 1) * size) + node.X);
                        }

                        if (h < 0.33f) heightGrid[node.X, node.Y] = 0.33f;   // Lakes are shallow
                    }
                }
            }

            // Add islands / higher areas to sometimes break up lakes
            var islandHeight = 2f;
            var islandGradient = 1f;

            for (int i = 0; i < islandFreq; i++)
            {
                var x = 10 + Rand.Next(size - 20);
                var y = 10 + Rand.Next(size - 20);
                if (heightGrid[x, y] > 0.5f) continue;
                heightGrid[x, y] += Rand.NextFloat() * islandHeight;

                var openNodes = new List<Vector2i> { new Vector2i(x, y) };
                var closedNodes = new HashSet<int> { (y * size) + x };
                while (openNodes.Any())
                {
                    var copy = openNodes.ToList();
                    openNodes.Clear();
                    foreach (var node in copy)
                    {
                        var h = heightGrid[node.X, node.Y];

                        if (node.X > 0 && !closedNodes.Contains((node.Y * size) + node.X - 1) && heightGrid[node.X - 1, node.Y] < h - islandGradient)
                        {
                            heightGrid[node.X - 1, node.Y] = h - (Rand.Next(2) == 0 ? Rand.NextFloat() * islandGradient : 0);
                            openNodes.Add(new Vector2i(node.X - 1, node.Y));
                            closedNodes.Add((node.Y * size) + node.X - 1);
                        }

                        if (node.X < size - 1 && !closedNodes.Contains((node.Y * size) + node.X + 1) && heightGrid[node.X + 1, node.Y] < h - islandGradient)
                        {
                            heightGrid[node.X + 1, node.Y] = h - (Rand.Next(2) == 0 ? Rand.NextFloat() * islandGradient : 0);
                            openNodes.Add(new Vector2i(node.X + 1, node.Y));
                            closedNodes.Add((node.Y * size) + node.X + 1);
                        }

                        if (node.Y > 0 && !closedNodes.Contains(((node.Y - 1) * size) + node.X) && heightGrid[node.X, node.Y - 1] < h - islandGradient)
                        {
                            heightGrid[node.X, node.Y - 1] = h - (Rand.Next(2) == 0 ? Rand.NextFloat() * islandGradient : 0);
                            openNodes.Add(new Vector2i(node.X, node.Y - 1));
                            closedNodes.Add(((node.Y - 1) * size) + node.X);
                        }

                        if (node.Y < size - 1 && !closedNodes.Contains(((node.Y + 1) * size) + node.X) && heightGrid[node.X, node.Y + 1] < h - islandGradient)
                        {
                            heightGrid[node.X, node.Y + 1] = h - (Rand.Next(2) == 0 ? Rand.NextFloat() * islandGradient : 0);
                            openNodes.Add(new Vector2i(node.X, node.Y + 1));
                            closedNodes.Add(((node.Y + 1) * size) + node.X);
                        }
                    }
                }
            }

            // Add small lakes
            for (int i = 0; i < pondFreq; i++)
            {
                var x = Rand.Next(size);
                var y = Rand.Next(size);
                if (heightGrid[x, y] < 0.5f) continue;

                heightGrid[x, y] = 0.33f;

                var openNodes = new List<Vector2i> { new Vector2i(x, y) };
                var closedNodes = new HashSet<int> { (y * size) + x };
                while (openNodes.Any())
                {
                    var copy = openNodes.ToList();
                    openNodes.Clear();
                    foreach (var node in copy)
                    {
                        if (node.X > 0 && !closedNodes.Contains((node.Y * size) + node.X - 1))
                        {
                            if (Rand.Next(2) != 0)
                            {
                                heightGrid[node.X - 1, node.Y] = 0.33f;
                                openNodes.Add(new Vector2i(node.X - 1, node.Y));
                            }

                            closedNodes.Add((node.Y * size) + node.X - 1);
                        }

                        if (node.X < size - 1 && !closedNodes.Contains((node.Y * size) + node.X + 1))
                        {
                            if (Rand.Next(2) != 0)
                            {
                                heightGrid[node.X + 1, node.Y] = 0.33f;
                                openNodes.Add(new Vector2i(node.X + 1, node.Y));
                            }

                            closedNodes.Add((node.Y * size) + node.X + 1);
                        }

                        if (node.Y > 0 && !closedNodes.Contains(((node.Y - 1) * size) + node.X))
                        {
                            if (Rand.Next(2) != 0)
                            {
                                heightGrid[node.X, node.Y - 1] = 0.33f;
                                openNodes.Add(new Vector2i(node.X, node.Y - 1));
                            }

                            closedNodes.Add(((node.Y - 1) * size) + node.X);
                        }

                        if (node.Y < size - 1 && !closedNodes.Contains(((node.Y + 1) * size) + node.X))
                        {
                            if (Rand.Next(2) != 0)
                            {
                                heightGrid[node.X, node.Y + 1] = 0.33f;
                                openNodes.Add(new Vector2i(node.X, node.Y + 1));
                            }

                            closedNodes.Add(((node.Y + 1) * size) + node.X);
                        }
                    }
                }
            }

            // Flatten
            for (int x = 0; x <= size; ++x)
            {
                for (int y = 0; y <= size; ++y)
                {
                    if (heightGrid[x, y] < 0f)
                    {
                        heightGrid[x, y] = 0f;
                    }
                }
            }

            foreach (var tile in generatedTemplate.BigTiles)
            {
                var wx = heightGrid[tile.TerrainX, tile.TerrainY];
                var sx = heightGrid[tile.TerrainX, tile.TerrainY + 1];
                var nx = heightGrid[tile.TerrainX + 1, tile.TerrainY];
                var ex = heightGrid[tile.TerrainX + 1, tile.TerrainY + 1];

                var w = (byte)Math.Round(wx);
                var s = (byte)Math.Round(sx);
                var n = (byte)Math.Round(nx);
                var e = (byte)Math.Round(ex);

                tile.TerrainType = TerrainType.Dirt;
                tile.BigTileTextureIdentifier = (BigTileTextureIdentifier)random.Next(4) + 1;

                if (w == 0 && s == 0 && n == 0 && e == 0)
                {
                    if (wx < 0.3f && sx < 0.3f && nx < 0.3f && ex < 0.3f)
                    {
                        tile.TerrainType = TerrainType.DeepWater;
                        tile.BigTileTextureIdentifier = BigTileTextureIdentifier.Deep;
                    }
                    else
                    {
                        tile.TerrainType = TerrainType.Water;
                        tile.BigTileTextureIdentifier = BigTileTextureIdentifier.Water;
                    }
                }
            }

            // Filter out any geometries that won't work
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var tile in generatedTemplate.BigTiles.Where(t => t.TerrainType == TerrainType.Dirt).ToList())
                {
                    if (tile.AdjacentTiles8.Any(t => t == null || t.TerrainType == TerrainType.DeepWater) || !CanTileBeLand(tile))
                    {
                        tile.TerrainType = TerrainType.Water;
                        tile.BigTileTextureIdentifier = BigTileTextureIdentifier.Water;
                        changed = true;
                    }
                }
            }

            foreach (var tile in generatedTemplate.SmallTiles.Values)
            {
                tile.TerrainType = tile.BigTile.TerrainType;
            }

            foreach (var tile in generatedTemplate.BigTiles.Where(t => t.TerrainType == TerrainType.Dirt).ToList())
            {
                var n = tile.TileToN;
                var e = tile.TileToE;
                var s = tile.TileToS;
                var w = tile.TileToW;
                var nw = tile.TileToNW;
                var ne = tile.TileToNE;
                var se = tile.TileToSE;
                var sw = tile.TileToSW;
                if (IsNullOrWater(nw?.TerrainType))
                {
                    var isCornerN = IsNullOrWater(ne?.TerrainType) || IsNullOrWater(e?.TerrainType);
                    var isCornerW = IsNullOrWater(sw?.TerrainType) || IsNullOrWater(s?.TerrainType);

                    tile.TerrainType = TerrainType.Coast;
                    if (isCornerN) tile.BigTileTextureIdentifier = (BigTileTextureIdentifier)random.Next(4) + 21;
                    else if (isCornerW) tile.BigTileTextureIdentifier = (BigTileTextureIdentifier)random.Next(4) + 45;
                    else tile.BigTileTextureIdentifier = (BigTileTextureIdentifier)random.Next(4) + 17;
                }
                else if (IsNullOrWater(se?.TerrainType))
                {
                    var isCornerE = IsNullOrWater(ne?.TerrainType) || IsNullOrWater(n?.TerrainType);
                    var isCornerS = IsNullOrWater(sw?.TerrainType) || IsNullOrWater(w?.TerrainType);

                    tile.TerrainType = TerrainType.Coast;
                    if (isCornerE && !isCornerS) tile.BigTileTextureIdentifier = (BigTileTextureIdentifier)random.Next(4) + 29;
                    else if (isCornerS && !isCornerE) tile.BigTileTextureIdentifier = (BigTileTextureIdentifier)random.Next(4) + 37;
                    else tile.BigTileTextureIdentifier = (BigTileTextureIdentifier)random.Next(4) + 9;
                }
                else if (IsNullOrWater(sw?.TerrainType))
                {
                    tile.TerrainType = TerrainType.Coast;

                    tile.BigTileTextureIdentifier = (BigTileTextureIdentifier)random.Next(4) + 5;
                    if (IsNullOrWater(e?.TerrainType)) tile.BigTileTextureIdentifier += 32;
                    else if (IsNullOrWater(n?.TerrainType)) tile.BigTileTextureIdentifier += 40;
                }
                else if (IsNullOrWater(ne?.TerrainType))
                {
                    tile.TerrainType = TerrainType.Coast;

                    tile.BigTileTextureIdentifier = (BigTileTextureIdentifier)random.Next(4) + 13;
                    if (IsNullOrWater(s?.TerrainType)) tile.BigTileTextureIdentifier += 16;
                    else if (IsNullOrWater(w?.TerrainType)) tile.BigTileTextureIdentifier += 8;
                }
                else if (IsNullOrWater(n?.TerrainType))
                {
                    var isBridgeTile = IsNullOrWater(s?.TerrainType) || IsNullOrWater(se?.TerrainType) || IsNullOrWater(sw?.TerrainType);
                    tile.TerrainType = TerrainType.Coast;
                    tile.BigTileTextureIdentifier = isBridgeTile ? BigTileTextureIdentifier.ChannelNS : (BigTileTextureIdentifier)random.Next(4) + 25;
                }
                else if (IsNullOrWater(w?.TerrainType))
                {
                    var isBridgeTile = IsNullOrWater(e?.TerrainType) || IsNullOrWater(se?.TerrainType) || IsNullOrWater(ne?.TerrainType);
                    tile.TerrainType = TerrainType.Coast;
                    tile.BigTileTextureIdentifier = isBridgeTile ? BigTileTextureIdentifier.ChannelEW : (BigTileTextureIdentifier)random.Next(4) + 49;
                }
                else if (IsNullOrWater(s?.TerrainType))
                {
                    tile.TerrainType = TerrainType.Coast;
                    tile.BigTileTextureIdentifier = (BigTileTextureIdentifier)random.Next(4) + 41;
                }
                else if (IsNullOrWater(e?.TerrainType))
                {
                    tile.TerrainType = TerrainType.Coast;
                    tile.BigTileTextureIdentifier = (BigTileTextureIdentifier)random.Next(4) + 33;
                }
            }

            foreach (var tile in generatedTemplate.BigTiles.Where(t => t.TerrainType == TerrainType.Water).ToList())
            {
                var n = tile.TileToN;
                var e = tile.TileToE;
                var s = tile.TileToS;
                var w = tile.TileToW;
                var nw = tile.TileToNW;
                var ne = tile.TileToNE;
                var se = tile.TileToSE;
                var sw = tile.TileToSW;
                if (nw == null || nw.TerrainType == TerrainType.DeepWater)
                {
                    var isCornerN = ne == null || e == null || ne?.TerrainType == TerrainType.DeepWater || e?.TerrainType == TerrainType.DeepWater;
                    var isCornerW = sw == null || s == null || sw?.TerrainType == TerrainType.DeepWater || s?.TerrainType == TerrainType.DeepWater;

                    tile.TerrainType = TerrainType.DeepWaterEdge;
                    if (isCornerN) tile.BigTileTextureIdentifier = BigTileTextureIdentifier.DeepN1 + random.Next(4);
                    else if (isCornerW) tile.BigTileTextureIdentifier = BigTileTextureIdentifier.DeepW1 + random.Next(4);
                    else tile.BigTileTextureIdentifier = BigTileTextureIdentifier.DeepNW1 + random.Next(4);
                }
                else if (se == null || se.TerrainType == TerrainType.DeepWater)
                {
                    var isCornerE = ne == null || n == null || ne?.TerrainType == TerrainType.DeepWater || n?.TerrainType == TerrainType.DeepWater;
                    var isCornerS = sw == null || w == null || sw?.TerrainType == TerrainType.DeepWater || w?.TerrainType == TerrainType.DeepWater;

                    tile.TerrainType = TerrainType.DeepWaterEdge;
                    if (isCornerE) tile.BigTileTextureIdentifier = BigTileTextureIdentifier.DeepE1 + random.Next(4);
                    else if (isCornerS) tile.BigTileTextureIdentifier = BigTileTextureIdentifier.DeepS1 + random.Next(4);
                    else tile.BigTileTextureIdentifier = BigTileTextureIdentifier.DeepSE1 + random.Next(4);
                }
                else if (sw == null || sw.TerrainType == TerrainType.DeepWater)
                {
                    tile.TerrainType = TerrainType.DeepWaterEdge;
                    tile.BigTileTextureIdentifier = BigTileTextureIdentifier.DeepSW1 + random.Next(4);

                    if (e == null || e.TerrainType == TerrainType.DeepWater) tile.BigTileTextureIdentifier += 32;
                    else if (n == null || n.TerrainType == TerrainType.DeepWater) tile.BigTileTextureIdentifier += 40;
                }
                else if (ne == null || ne.TerrainType == TerrainType.DeepWater)
                {
                    tile.TerrainType = TerrainType.DeepWaterEdge;
                    tile.BigTileTextureIdentifier = BigTileTextureIdentifier.DeepNE1 + random.Next(4);

                    if (s == null || s.TerrainType == TerrainType.DeepWater) tile.BigTileTextureIdentifier += 16;
                    else if (w == null || w.TerrainType == TerrainType.DeepWater) tile.BigTileTextureIdentifier += 8;
                }
                else if (n == null || n.TerrainType == TerrainType.DeepWater)
                {
                    var isBridgeTile = s?.TerrainType == TerrainType.DeepWater || se?.TerrainType == TerrainType.DeepWater || sw?.TerrainType == TerrainType.DeepWater;
                    tile.TerrainType = TerrainType.DeepWaterEdge;
                    tile.BigTileTextureIdentifier = isBridgeTile ? BigTileTextureIdentifier.DeepChannelNS : BigTileTextureIdentifier.DeepN5 + random.Next(4);
                }
                else if (w == null || w.TerrainType == TerrainType.DeepWater)
                {
                    var isBridgeTile = e?.TerrainType == TerrainType.DeepWater || se?.TerrainType == TerrainType.DeepWater || ne?.TerrainType == TerrainType.DeepWater;
                    tile.TerrainType = TerrainType.DeepWaterEdge;
                    tile.BigTileTextureIdentifier = isBridgeTile ? BigTileTextureIdentifier.DeepChannelEW : BigTileTextureIdentifier.DeepW5 + random.Next(4);
                }
                else if (s == null || s.TerrainType == TerrainType.DeepWater)
                {
                    tile.TerrainType = TerrainType.DeepWaterEdge;
                    tile.BigTileTextureIdentifier = BigTileTextureIdentifier.DeepS5 + random.Next(4);
                }
                else if (e == null || e.TerrainType == TerrainType.DeepWater)
                {
                    tile.TerrainType = TerrainType.DeepWaterEdge;
                    tile.BigTileTextureIdentifier = BigTileTextureIdentifier.DeepE5 + random.Next(4);
                }

                // Workaround for glitchy underwater corner tiles
                if (tile.TerrainType == TerrainType.DeepWaterEdge)
                {
                    if (tile.BigTileTextureIdentifier.In(BigTileTextureIdentifier.DeepE1, BigTileTextureIdentifier.DeepE1, BigTileTextureIdentifier.DeepS1, BigTileTextureIdentifier.DeepW1))
                    {
                        tile.BigTileTextureIdentifier += random.Next(3) + 1;
                    }
                }
            }

            foreach (var tile in this.generatedTemplate.BigTiles) tile.UpdateSmallTileTerrainTypes();
        }

        protected override void AddBiomes(Random random)
        {
            var openNodes = new Queue<SmallTileTemplate>();
            var closedNodes = new HashSet<int>();
            var blobsToDump = 158;
            var tileCount = this.generatedTemplate.SmallTiles.Count;

            while (blobsToDump > 146)
            {
                var tileIndex = random.Next(tileCount);
                var tile = this.generatedTemplate.GetSmallTile(tileIndex);
                tile.BiomeType = BiomeType.Desert;
                tile.SoilTypeCount = 10000 + random.Next(20000);
                openNodes.Enqueue(tile);

                blobsToDump--;
            }

            while (blobsToDump > 0)
            {
                blobsToDump--;

                var tileIndex = random.Next(tileCount);
                var tile = this.generatedTemplate.GetSmallTile(tileIndex);
                if (tile.BiomeType != BiomeType.Dry) continue;

                var type = BiomeType.Forest;
                if (tile.TerrainType != TerrainType.Dirt)
                {
                    type = BiomeType.Wet;
                }
                else
                {
                    if (blobsToDump > 56) continue;
                    var rand = random.NextDouble();
                    if (rand > 0.95) type = BiomeType.Wet;
                    else if (rand > 0.60) type = BiomeType.Grass;
                    else if (rand > 0.30) type = BiomeType.SmallPlants;
                }

                tile.BiomeType = type;
                tile.SoilTypeCount = 5000 + random.Next(15000);
                if (type == BiomeType.Grass) tile.SoilTypeCount *= 2;
                else if (type == BiomeType.Wet) tile.SoilTypeCount /= 2;
                openNodes.Enqueue(tile);
            }

            while (openNodes.Any())
            {
                var t = openNodes.Dequeue();
                if (t.SoilTypeCount <= 32)
                {
                    if (t.BiomeType == BiomeType.Grass && random.Next(2) == 0)
                    {
                        // Redfruits like to grow on the edge of grass forests
                        t.BiomeType = BiomeType.GrassEdge;
                    }

                    closedNodes.Add(t.Index);
                    continue;
                }

                var toMove = (t.SoilTypeCount - 64) + random.Next(t.SoilTypeCount > 64 ? 24 : t.SoilTypeCount / 3);
                if (toMove <= 0)
                {
                    closedNodes.Add(t.Index);
                    continue;
                }

                var adj = t.AdjacentTiles8.Where(x => !closedNodes.Contains(x.Index)).ToList();
                while (toMove > 0 && adj.Count > 0)
                {
                    var a = adj[random.Next(adj.Count)];
                    t.SoilTypeCount--;
                    a.BiomeType = t.BiomeType;
                    a.SoilTypeCount++;
                    toMove--;
                }

                foreach (var a in adj) openNodes.Enqueue(a);

                closedNodes.Add(t.Index);
            }
        }

        protected override void AddGroundCover()
        {
            foreach (var tile in this.generatedTemplate.SmallTiles.Values.Where(t => t.TerrainType == TerrainType.Dirt && t.BiomeType != BiomeType.Desert))
            {
                var density = 0;
                if (tile.BiomeType == BiomeType.Grass)
                {
                    density = tile.AdjacentTiles8.Count(t => t.BiomeType != BiomeType.Desert && t.BiomeType != BiomeType.Grass);
                }
                else
                {
                    density = 8 - tile.AdjacentTiles8.Count(t => t.BiomeType == BiomeType.Desert);
                }

                if (tile.BiomeType == BiomeType.Wet) density *= 2;

                tile.GroundCoverDensity = density;
                tile.GroundCoverMaxDensity = density;
                tile.GroundCoverDirection = (Direction)(Rand.Next(4) + 4);
            }

            // Spread onto coast tiles
            foreach (var tile in this.generatedTemplate.SmallTiles.Values.Where(t => t.TerrainType == TerrainType.Coast))
            {
                var adj = tile.AdjacentTiles4.Where(t => t.TerrainType == TerrainType.Dirt).ToList();
                if (adj.Any()) tile.GroundCoverDensity = (int)(adj.Average(t => t.GroundCoverDensity));
            }
        }

        protected override void AddPlants(Random random)
        {
            Log("Adding plants");
            PlantGrowthController.Clear();
            foreach (var tile in this.generatedTemplate.SmallTiles.Values.Where(t => t.TerrainType == TerrainType.Dirt && !t.ThingsAll.Any()))
            {
                if (tile.BiomeType == BiomeType.Desert)
                {
                    if (random.Next(120) == 0) this.generatedTemplate.AddPlant(tile, ThingType.SmallPlant7);
                    continue;
                }

                if (tile.BiomeType != BiomeType.Wet && tile.BiomeType != BiomeType.Grass && random.NextDouble() > 0.99)
                {
                    if (random.Next(2) == 0 && IsAreaEmpty(tile.X, tile.Y, tile.X + 1, tile.Y + 1))
                    {
                        if (random.Next(5) == 0) PlantGrowthController.AddSeed(ThingType.Bush, tile.Index, random.Next(3600));
                        else this.generatedTemplate.AddPlant(tile, ThingType.Bush);
                    }
                    else
                    {
                        // Small plant 5 (bluefruit)
                        if (random.Next(2) == 0) PlantGrowthController.AddSeed(ThingType.SmallPlant5, tile.Index, random.Next(3600));
                        else this.generatedTemplate.AddPlant(tile, ThingType.SmallPlant5);
                    }

                    continue;
                }

                if (tile.BiomeType == BiomeType.GrassEdge && random.Next(4) == 0)
                {
                    // Small plant 6 (redfruit)
                    if (random.Next(3) == 0) PlantGrowthController.AddSeed(ThingType.SmallPlant6, tile.Index, random.Next(3600));
                    else this.generatedTemplate.AddPlant(tile, ThingType.SmallPlant6);

                    continue;
                }

                // Small plants
                var rand = random.NextDouble();
                if (tile.BiomeType == BiomeType.SmallPlants) rand = 1.0 - ((1.0 - rand) * 0.04);

                if (tile.BiomeType == BiomeType.Wet)
                {
                    if (tile.AdjacentTiles8.Any(t => t.TerrainType == TerrainType.Coast))
                    {
                        var r = Rand.Next(8);
                        for (int i = 0; i < r; i++)
                        {
                            var offset = GetPositionOnTileForNewCoastGrass(tile);
                            if (offset != null) this.generatedTemplate.AddPlant(tile, ThingType.CoastGrass);
                        }
                    }
                    else if (rand > 0.9) this.generatedTemplate.AddPlant(tile, ThingType.SmallPlant4);
                    else if (rand > 0.7) this.generatedTemplate.AddPlant(tile, ThingType.SmallPlant8);
                }
                else if (rand > 0.99) PlantGrowthController.AddSeed(ThingType.SmallPlant1, tile.Index, random.Next(3600));
                else if (rand > 0.988) PlantGrowthController.AddSeed(ThingType.SmallPlant2, tile.Index, random.Next(3600));
                else if (rand > 0.986) this.generatedTemplate.AddPlant(tile, ThingType.SmallPlant2);
                else if (rand > 0.984) PlantGrowthController.AddSeed(ThingType.SmallPlant3, tile.Index, random.Next(3600));
                else if (rand > 0.982) this.generatedTemplate.AddPlant(tile, ThingType.SmallPlant3);
                else if (tile.BiomeType == BiomeType.Grass) this.generatedTemplate.AddPlant(tile, ThingType.Grass);
                else if (tile.BiomeType == BiomeType.Forest && random.NextDouble() > 0.95 
                    && tile.AdjacentTiles8.SelectMany(t => t.ThingsPrimary).All(t => t.ThingType != ThingType.Tree))
                {
                    this.generatedTemplate.AddPlant(tile, ThingType.Tree);
                }
            }

            // Spread bluefruit around a bit into patches
            foreach (var plant in this.generatedTemplate.GetPlants().Where(p => p.ThingType == ThingType.SmallPlant5))
            {
                var openNodes = new Queue<int>();
                var closedNodes = new HashSet<int>();
                var count = 0;

                closedNodes.Add(plant.MainTileIndex);
                foreach (var a in plant.MainTile.AdjacentTiles8) openNodes.Enqueue(a.Index);

                while (openNodes.Any() && count < 20)
                {
                    var i = openNodes.Dequeue();

                    var t = generatedTemplate.GetSmallTile(i);
                    if (t.TerrainType == TerrainType.Dirt && !t.ThingsAll.Any() && !t.BiomeType.In(BiomeType.Wet, BiomeType.Grass, BiomeType.Desert))
                    {
                        var r = random.Next(4);
                        if (r == 0) PlantGrowthController.AddSeed(ThingType.SmallPlant5, i, random.Next(3600));
                        else if (r == 1) generatedTemplate.AddPlant(t, ThingType.SmallPlant5);

                        var adj = t.AdjacentTiles8.Where(x => !closedNodes.Contains(x.Index)).ToList();
                        foreach (var a in adj) openNodes.Enqueue(a.Index);
                    }

                    closedNodes.Add(t.Index);
                    count++;
                }
            }

            var tilesToPlant = new Dictionary<SmallTileTemplate, ThingType>();
            foreach (var plant in this.generatedTemplate.GetPlants().Where(p => p.ThingType == ThingType.SmallPlant4 || p.ThingType == ThingType.SmallPlant8))
            {
                var openNodes = new Queue<int>();
                var closedNodes = new HashSet<int>();
                var count = 0;
                var max = plant.ThingType == ThingType.SmallPlant4 ? 10 : 20;

                closedNodes.Add(plant.MainTileIndex);
                foreach (var a in plant.MainTile.AdjacentTiles8) openNodes.Enqueue(a.Index);

                while (openNodes.Any() && count < max)
                {
                    var i = openNodes.Dequeue();

                    var t = this.generatedTemplate.GetSmallTile(i);
                    if (t.TerrainType == TerrainType.Dirt && !t.ThingsAll.Any() && !tilesToPlant.ContainsKey(t) && t.BiomeType == BiomeType.Wet)
                    {
                        tilesToPlant.Add(t, plant.ThingType);

                        var adj = t.AdjacentTiles8.Where(x => !closedNodes.Contains(x.Index)).ToList();
                        foreach (var a in adj) openNodes.Enqueue(a.Index);
                    }

                    closedNodes.Add(t.Index);
                    count++;
                }
            }

            foreach (var kv in tilesToPlant)
            {
                if (Rand.Next(5) == 0) PlantGrowthController.AddSeed(kv.Value, kv.Key.Index, random.Next(3600));
                else this.generatedTemplate.AddPlant(kv.Key as SmallTileTemplate, kv.Value);
            }
        }

        protected override void AddOre(Dictionary<int, Tuple<ItemType, int>> resourcesByTile, Random random)
        {
            this.AddOre(resourcesByTile, random, 25, 400, 1400, 4);
            this.AddOre(resourcesByTile, random, 25, 300, 1200, 6);
            this.AddOre(resourcesByTile, random, 50, 40, 1000, 8);
            this.AddOre(resourcesByTile, random, 50, 60, 800, 12);
            this.AddOre(resourcesByTile, random, 50, 80, 640, 16);
            this.AddOre(resourcesByTile, random, 50, 80, 450, 18);
            this.AddOre(resourcesByTile, random, 50, 80, 400, 20);
            this.AddOre(resourcesByTile, random, 50, 40, 200, 20);
        }

        protected override int FindLandingZone()
        {
            SmallTileTemplate bestTile = null;
            var bestScore = 0;
            var candidateCount = 0;
            foreach (var tile in this.generatedTemplate.SmallTiles.Values)
            {
                // Discard tiles with water, plants or rocks right away
                if (tile.TerrainType != TerrainType.Dirt || tile.ThingsAll.Any()) continue;

                // Immediate area must be flat, have no more than 3 plants or rocks, and at least 20 iron ore
                var thingCount = 0;
                var oreCount = 0;
                var valid = true;
                foreach (var t in tile.AdjacentTiles8.SelectMany(x => x.AdjacentTiles4).SelectMany(x => x.AdjacentTiles4).SelectMany(x => x.AdjacentTiles4).Distinct())
                {
                    if (t.TerrainType != TerrainType.Dirt)
                    {
                        valid = false;
                        break;
                    }

                    thingCount += t.ThingsAll.Count;
                    if (thingCount > 3) break;

                    if (t.MineResourceType == ItemType.IronOre && t.MineResourceCount > 4) oreCount += t.MineResourceCount;
                }

                if (!valid || oreCount < 20 || thingCount > 3) continue;

                var score = GetStartTileScore(tile);
                if (score > 0)
                {
                    candidateCount++;
                    if (score > bestScore)
                    {
                        bestTile = tile;
                        bestScore = score;
                    }
                }
            }

            return bestTile?.Index ?? 0;
        }

        protected override int GetStartTileScore(SmallTileTemplate tile)
        {
            var stats = this.GetLandingZoneStats(tile, 20);
            if (stats.Ore <= 40 || stats.Stone < 40) return 0;   // Must have ore and stone rocks nearby

            return 1000 - Math.Abs(600 - stats.Space);   // Start places with less space tend to be more interesting, we'll aim for 600 tiles
        }

        private static Vector2f GetPositionOnTileForNewCoastGrass(SmallTileTemplate tile)
        {
            if (tile.ThingsAll.Any(t => t.ThingType != ThingType.CoastGrass) || tile.ThingsAll.Count > 2) return null;

            // 8 attempts to find a free position
            for (int i = 0; i < 8; i++)
            {
                var ok = true;
                var vec = new Vector2f((float)(Rand.NextDouble() * 0.8f) - 0.4f, (float)(Rand.NextDouble() * 0.8f) - 0.4f);
                foreach (var p in tile.ThingsPrimary.OfType<CoastGrass>())
                {
                    if ((p.PositionOffset - vec).Length() < 0.5f)
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok) return vec;
            }

            return null;
        }
    }
}
