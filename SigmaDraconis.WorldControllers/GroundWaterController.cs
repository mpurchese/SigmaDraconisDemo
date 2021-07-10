namespace SigmaDraconis.WorldControllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Shared;
    using World;
    using WorldInterfaces;

    public static class GroundWaterController
    {
        private static Dictionary<int, TileGroundWaterDetail> groundWaterByTile = new Dictionary<int, TileGroundWaterDetail>();

        public static int GetTileExtractionRate(ISmallTile tile, bool includeAdjacent)
        {
            if (tile == null) return 0;

            if (!groundWaterByTile.ContainsKey(tile.Index)) InitTile(tile);
            var w = groundWaterByTile[tile.Index];

            var result = (int)Math.Round(100 * w.CurrentLevel / (float)Constants.MaxGroundWaterNormal);
            if (includeAdjacent)
            {
                foreach (var tile2 in tile.AdjacentTiles8) result += GetTileExtractionRate(tile2, false);
                result += GetTileExtractionRate(tile, false);
                result /= 10;
            }

            return result;
        }

        public static int TryTakeFromTile(ISmallTile tile)
        {
            if (tile == null) return 0;

            var rate = GetTileExtractionRate(tile, false);
            if (rate == 0 || Rand.Next(100) + 1 > rate) return 0;

            var w = groundWaterByTile[tile.Index];
            w.CurrentLevel--;
            if (tile.BiomeType == BiomeType.Wet && w.CurrentLevel <= Constants.MaxGroundWaterNormal)
            {
                tile.BiomeType = BiomeType.Dry;
                EventManager.EnqueueWorldPropertyChangeEvent(tile.Index, nameof(ISmallTile.BiomeType));

                if (tile.GroundCoverDensity > 8)
                {
                    World.TilesWithGroundCoverRemovedQueue.Enqueue(new Tuple<int, int>(tile.Index, tile.GroundCoverDensity));
                    tile.GroundCoverDensity = 8;
                    EventManager.EnqueueWorldPropertyChangeEvent(tile.Index, nameof(ISmallTile.GroundCoverDensity));
                }
            }
            else if (tile.BiomeType != BiomeType.Desert && w.CurrentLevel <= Constants.MaxGroundWaterDry)
            {
                tile.BiomeType = BiomeType.Desert;
                EventManager.EnqueueWorldPropertyChangeEvent(tile.Index, nameof(ISmallTile.BiomeType));

                if (tile.GroundCoverDensity > 0)
                {
                    World.TilesWithGroundCoverRemovedQueue.Enqueue(new Tuple<int, int>(tile.Index, tile.GroundCoverDensity));
                    tile.GroundCoverDensity = 0;
                    EventManager.EnqueueWorldPropertyChangeEvent(tile.Index, nameof(ISmallTile.GroundCoverDensity));
                }
            }

            return 1;
        }

        public static void Clear()
        {
            groundWaterByTile.Clear();
        }

        public static void Update()
        {
            // Respond to pump requests
            if ((World.WorldTime.FrameNumber + 7) % 10 == 0)
            {
                foreach (var pump in World.GetThings<IWaterPump>(ThingType.WaterPump))
                {
                    pump.ExtractionRate = GetTileExtractionRate(pump.MainTile, true);
                    if (pump.RequestingWaterFromGroundTile.HasValue && pump.FactoryStatus == FactoryStatus.InProgress)
                    {
                        var tile = World.GetSmallTile(pump.RequestingWaterFromGroundTile.Value);
                        TryTakeFromTile(tile);
                        pump.RequestingWaterFromGroundTile = 0;
                    }
                }
            }

            if ((World.WorldTime.FrameNumber + 7) % 60 != 0) return;

            // Slowly replace water
            foreach (var tileIndex in groundWaterByTile.Keys.Where(k => Rand.Next(1000) < Constants.GroundWaterReplenishRate).ToList())
            {
                var tile = World.GetSmallTile(tileIndex);
                if (tile.ThingsPrimary.Any(t => t is IFoundation || t.ThingType == ThingType.Roof)) continue;

                var w = groundWaterByTile[tileIndex];
                if (w.CurrentLevel < w.MaxLevel)
                {
                    w.CurrentLevel++;
                    if (tile.BiomeType == BiomeType.Desert && w.OriginalBiome != BiomeType.Desert && w.CurrentLevel > Constants.MaxGroundWaterDry)
                    {
                        tile.BiomeType = w.OriginalBiome == BiomeType.Wet ? BiomeType.Dry : w.OriginalBiome;
                        EventManager.EnqueueWorldPropertyChangeEvent(tileIndex, nameof(ISmallTile.BiomeType));
                    }
                    else if (tile.BiomeType == BiomeType.Dry && w.OriginalBiome == BiomeType.Wet && w.CurrentLevel > Constants.MaxGroundWaterNormal)
                    {
                        tile.BiomeType = BiomeType.Wet;
                        EventManager.EnqueueWorldPropertyChangeEvent(tileIndex, nameof(ISmallTile.BiomeType));
                    }

                    if (w.CurrentLevel == w.MaxLevel) groundWaterByTile.Remove(tileIndex);   // Don't need to track tile any more once fully replenished
                }
            }
        }

        public static Dictionary<int, TileGroundWaterDetail> Serialize()
        {
            return groundWaterByTile;
        }

        public static void Deserialize(Dictionary<int, TileGroundWaterDetail> obj, GameVersion version)
        {
            groundWaterByTile = obj;
            if (version.Major == 0 && version.Minor < 5)
            {
                foreach (var kv in groundWaterByTile)
                {
                    var tile = World.GetSmallTile(kv.Key);
                    if (tile.BiomeType == BiomeType.Wet)
                    {
                        kv.Value.CurrentLevel = (int)(kv.Value.CurrentLevel * 1.4);
                        kv.Value.MaxLevel = (int)(kv.Value.MaxLevel * 1.4);
                    }
                }
            }
        }

        private static void InitTile(ISmallTile tile)
        {
            var level = Constants.MaxGroundWaterNormal;
            if (tile.BiomeType == BiomeType.Desert) level = Constants.MaxGroundWaterDry;
            else if (tile.BiomeType == BiomeType.Wet) level = Constants.MaxGroundWaterWet;
            groundWaterByTile.Add(tile.Index, new TileGroundWaterDetail(level, level, tile.BiomeType));
        }
    }
}
