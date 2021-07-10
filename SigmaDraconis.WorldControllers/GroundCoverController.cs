namespace SigmaDraconis.WorldControllers
{
    using System.Collections.Generic;
    using System.Linq;
    using Shared;
    using World;
    using WorldInterfaces;

    public static class GroundCoverController
    {
        // Value is the original density
        private static Dictionary<int, int> tilesWithGroundCoverRemoved = new Dictionary<int, int>();

        public static void Clear()
        {
            tilesWithGroundCoverRemoved.Clear();
        }

        public static void Update()
        {
            while (World.TilesWithGroundCoverRemovedQueue.Any())
            {
                var tuple = World.TilesWithGroundCoverRemovedQueue.Dequeue();
                if (!tilesWithGroundCoverRemoved.ContainsKey(tuple.Item1)) tilesWithGroundCoverRemoved.Add(tuple.Item1, tuple.Item2);
            }

            // Every 0.1 hours during the day, restore 1% of missing ground cover
            if ((World.WorldTime.FrameNumber + 8) % 360 == 0 && World.Temperature > 0 && World.WorldLight.Brightness > 0.2)
            {
                foreach (var tileIndex in tilesWithGroundCoverRemoved.Keys.Where(t => Rand.Next(100) == 0).ToList())
                {
                    var tile = World.GetSmallTile(tileIndex);
                    if (tile.BiomeType != BiomeType.Desert && tile.BiomeType != BiomeType.Grass && tile.ThingsAll.All(t => !(t is IFoundation)))
                    {
                        var max = tilesWithGroundCoverRemoved[tileIndex];
                        if (max > 8 && tile.BiomeType != BiomeType.Wet) max = 8;
                        if (tile.GroundCoverDensity < max)
                        {
                            tile.GroundCoverDensity++;
                            EventManager.EnqueueWorldPropertyChangeEvent(tileIndex, nameof(ISmallTile.GroundCoverDensity));
                        }

                        if (tile.GroundCoverDensity >= tilesWithGroundCoverRemoved[tileIndex]) tilesWithGroundCoverRemoved.Remove(tileIndex);
                    }
                }
            }
        }

        public static Dictionary<int, int> Serialize()
        {
            return tilesWithGroundCoverRemoved;
        }

        public static void Deserialize(Dictionary<int, int> obj)
        {
            tilesWithGroundCoverRemoved = obj;
        }
    }
}
