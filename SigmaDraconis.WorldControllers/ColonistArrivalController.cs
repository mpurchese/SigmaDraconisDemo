namespace SigmaDraconis.WorldControllers
{
    using System.Collections.Generic;
    using System.Linq;

    using Shared;

    using World;
    using WorldInterfaces;

    public class ColonistArrivalController
    {
        public static ISmallTile ChooseLandingCoord()
        {
            var lander = World.GetThings<ILander>(ThingType.Lander).FirstOrDefault();
            if (lander == null) return null;

            var openNodes = new List<int> { lander.MainTileIndex };
            var closedNodes = new HashSet<int> { lander.MainTileIndex };
            var distance = 0;
            int score = 0;
            var bestCandidates = new List<int>();
            while (openNodes.Any())
            {
                foreach (var n in openNodes.ToList())
                {
                    openNodes.Remove(n);

                    var t = World.GetSmallTile(n);
                    var s = GetTileScore(t, distance);
                    if (s > 0 && s >= score)
                    {
                        if (s > score) bestCandidates.Clear();
                        score = s;
                        bestCandidates.Add(n);
                    }

                    foreach (var i in t.AdjacentTiles8.Where(a => a.TerrainType == TerrainType.Dirt).Select(l => l.Index))
                    {
                        if (!closedNodes.Contains(i))
                        {
                            openNodes.Add(i);
                            closedNodes.Add(i);
                        }
                    }
                }

                distance++;
                if (distance > 10 && bestCandidates.Any())
                {
                    openNodes.Clear();
                }

                if (distance >= 100) return null;
            }

            if (!bestCandidates.Any()) return null;
            return World.GetSmallTile(bestCandidates[Rand.Next(bestCandidates.Count)]);
        }

        private static int GetTileScore(ISmallTile tile, int distance)
        {
            if (tile.ThingsAll.Any() || tile.TerrainType != TerrainType.Dirt) return 0;
            if (tile.AdjacentTiles8.SelectMany(t => t.ThingsAll).Any()) return 0;

            var score = 100 - distance;
            score -= tile.AdjacentTiles8.SelectMany(t => t.AdjacentTiles8).SelectMany(t => t.ThingsAll).Count();

            return score;
        }
    }
}
