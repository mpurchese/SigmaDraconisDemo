using System;
using System.Collections.Generic;
using System.Linq;

namespace SigmaDraconis.World
{
    public static class WorldStats
    {
        private static readonly Dictionary<string, long> stats = new Dictionary<string, long>();
        public static event EventHandler<WorldStatsEventArgs> StatIncremented;

        public static void Reset()
        {
            stats.Clear();
        }

        public static long Get(string key)
        {
            return stats.ContainsKey(key) ? stats[key] : 0;
        }

        public static void Set(string key, long value)
        {
            if (!stats.ContainsKey(key)) stats.Add(key, value);
            else stats[key] = value;
        }

        public static void Increment(string key, long value = 1)
        {
            if (!stats.ContainsKey(key)) stats.Add(key, value);
            else stats[key] += value;

            StatIncremented?.Invoke(null, new WorldStatsEventArgs(key));
        }

        public static Dictionary<string, long> Serialize()
        {
            return stats.ToDictionary(s => s.Key, s => s.Value);
        }

        public static void Deserialize(Dictionary<string, long> obj)
        {
            stats.Clear();
            foreach (var kv in obj)
            {
                stats.Add(kv.Key, kv.Value);
            }
        }

        public static bool AllCropTypesHarvested()
        {
            return Get(WorldStatKeys.CropsHarvested1) > 0
                && Get(WorldStatKeys.CropsHarvested2) > 0
                && Get(WorldStatKeys.CropsHarvested3) > 0
                && Get(WorldStatKeys.CropsHarvested4) > 0
                && Get(WorldStatKeys.CropsHarvested5) > 0;
        }

        public static int CountCropTypesHarvested()
        {
            return (Get(WorldStatKeys.CropsHarvested1) > 0 ? 1 : 0)
                + (Get(WorldStatKeys.CropsHarvested2) > 0 ? 1 : 0)
                + (Get(WorldStatKeys.CropsHarvested3) > 0 ? 1 : 0)
                + (Get(WorldStatKeys.CropsHarvested4) > 0 ? 1 : 0)
                + (Get(WorldStatKeys.CropsHarvested5) > 0 ? 1 : 0);
        }
    }
}
