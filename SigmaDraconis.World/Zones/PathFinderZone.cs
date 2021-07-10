namespace SigmaDraconis.World.Zones
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Config;
    using Shared;
    using WorldInterfaces;

    public class PathFinderZone
    {
        public TileBlockType TileBlockType { get; }
        
        public PathFinderZone(TileBlockType tileBlockType)
        {
            this.TileBlockType = tileBlockType;
        }

        public Dictionary<int, IPathFinderNode> Nodes { get; set; } = new Dictionary<int, IPathFinderNode>();

        public PathFinderNode AddNode(int tileIndex)
        {
            var tile = World.GetSmallTile(tileIndex);
            var node = new PathFinderNode(tileIndex, tile.X, tile.Y);
            this.Nodes.Add(tileIndex, node);
            this.UpdateNode(node, tileIndex);
            return node;
        }

        public void RemoveNode(int tileIndex)
        {
            var tile = World.GetSmallTile(tileIndex);

            if (tile.TileToNE != null && this.Nodes.ContainsKey(tile.TileToNE.Index))
            {
                this.Nodes[tile.TileToNE.Index] = null;
            }

            if (tile.TileToSE != null && this.Nodes.ContainsKey(tile.TileToSE.Index))
            {
                this.Nodes[tile.TileToSE.Index] = null;
            }

            if (tile.TileToSW != null && this.Nodes.ContainsKey(tile.TileToSW.Index))
            {
                this.Nodes[tile.TileToSW.Index] = null;
            }

            if (tile.TileToNW != null && this.Nodes.ContainsKey(tile.TileToNW.Index))
            {
                this.Nodes[tile.TileToNW.Index] = null;
            }

            if (tile.TileToN != null && this.Nodes.ContainsKey(tile.TileToN.Index))
            {
                this.Nodes[tile.TileToN.Index] = null;
            }

            if (tile.TileToE != null && this.Nodes.ContainsKey(tile.TileToE.Index))
            {
                this.Nodes[tile.TileToE.Index] = null;
            }

            if (tile.TileToS != null && this.Nodes.ContainsKey(tile.TileToS.Index))
            {
                this.Nodes[tile.TileToS.Index] = null;
            }

            if (tile.TileToW != null && this.Nodes.ContainsKey(tile.TileToW.Index))
            {
                this.Nodes[tile.TileToW.Index] = null;
            }

            this.Nodes.Remove(tileIndex);
        }

        /// <summary>
        /// Update node and links
        /// </summary>
        public void UpdateNode(int tileIndex)
        {
            if (this.Nodes.ContainsKey(tileIndex))
            {
                this.UpdateNode(this.Nodes[tileIndex], tileIndex);
            }
        }

        /// <summary>
        /// Update node and links
        /// </summary>
        public void UpdateNode(IPathFinderNode node, int tileIndex)
        {
            var tile = World.GetSmallTile(tileIndex);

            for (int i = 0; i < 8; i++)
            {
                var direction = (Direction)i;
                var adjacentTile = tile.GetTileToDirection(direction);
                if (adjacentTile == null) continue;

                UpdateTileLink(node, tile, adjacentTile, direction);
                if (this.Nodes.ContainsKey(adjacentTile.Index)) UpdateTileLink(this.Nodes[adjacentTile.Index], adjacentTile, tile, DirectionHelper.Reverse(direction)); 
            }
        }

        private void UpdateTileLink(IPathFinderNode node, ISmallTile tile, ISmallTile adjacentTile, Direction direction)
        {
            
            var cost = -1;
            var diagonal = (int)direction < 4;
            if (adjacentTile != null && this.Nodes.ContainsKey(adjacentTile.Index) && !PathFinderBlockManager.IsBlocked(tile.Index, direction, this.TileBlockType))
            {
                node.SetLink(direction, this.Nodes[adjacentTile.Index]);
                if (this.TileBlockType == TileBlockType.Colonist)
                {
                    if (adjacentTile.ThingsPrimary.Any(t => t.Definition != null && t.Definition.TileBlockModel == TileBlockModel.Point))
                    {
                        // Tree / grass penalty
                        cost = diagonal ? 282 : 200;
                    }
                    else
                    {
                        // Foundation bonus
                        var hasFoundation = adjacentTile.ThingsPrimary.Any(t => t.ThingType.IsFoundation());
                        if (diagonal) cost = hasFoundation ? 118 : 141;
                        else cost = hasFoundation ? 83 : 100;
                    }
                }
                else cost = diagonal ? 141 : 100;

                //this.Nodes[adjacentTile.Index].SetLink(DirectionHelper.Reverse(direction), node);
            }
            else
            {
                node.SetLink(direction, null);
                //if (adjacentTile != null && this.Nodes.ContainsKey(adjacentTile.Index)) this.Nodes[adjacentTile.Index].SetLink(DirectionHelper.Reverse(direction), null);
            }

            switch (direction)
            {
                case Direction.N: node.CostN = cost; break;
                case Direction.NE: node.CostNE = cost; break;
                case Direction.E: node.CostE = cost; break;
                case Direction.SE: node.CostSE = cost; break;
                case Direction.S: node.CostS = cost; break;
                case Direction.SW: node.CostSW = cost; break;
                case Direction.W: node.CostW = cost; break;
                case Direction.NW: node.CostNW = cost; break;
            }
        }

        /// <summary>
        /// Find the best tile in the zone, based on a supplied scoring function
        /// </summary>
        public int? FindBestTile(IAnimal client, int minScore, int targetScore, int maxDistance, int minTemperature, Func<IAnimal, ISmallTile, int, int, int> scoreFunc, out int? score)
        {
            return FindBestTile(client, minScore, targetScore, maxDistance, minTemperature, null, scoreFunc, out score);
        }

        /// <summary>
        /// Find the best tile in the zone, based on a supplied scoring function
        /// </summary>
        public int? FindBestTile(IAnimal client, int minScore, int targetScore, int maxDistance, int minTemperature, HashSet<int> accessibleTiles, Func<IAnimal, ISmallTile, int, int, int> scoreFunc, out int? score)
        {
            score = null;
            var bestCandidates = new List<int>();
            int startTileIndex = client.MainTileIndex;
            if (!this.ContainsNode(startTileIndex)) return null;

            var openNodes = new List<int> { startTileIndex };
            var closedNodes = new HashSet<int> { startTileIndex };
            var distance = 0;
            while (openNodes.Any() && distance <= maxDistance && (score ?? 0) < targetScore)
            {
                foreach (var n in openNodes.ToList())
                {
                    openNodes.Remove(n);

                    var t = World.GetSmallTile(n);
                    var s = scoreFunc(client, t, distance, minTemperature);
                    if (s >= minScore && (score == null || s >= score))
                    {
                        if (s > score) bestCandidates.Clear();
                        score = s;
                        bestCandidates.Add(n);
                    }

                    if (score == null || score < targetScore)
                    {
                        var node = this.Nodes[n];
                        foreach (var i in node.AllLinks.Select(l => l.Index))
                        {
                            if (!closedNodes.Contains(i))
                            {
                                if (accessibleTiles == null || accessibleTiles.Contains(i)) openNodes.Add(i);
                                closedNodes.Add(i);
                            }
                        }
                    }
                }

                distance++;
            }

            return bestCandidates.Any() ? bestCandidates[Rand.Next(bestCandidates.Count)] : (int?)null;
        }

        public void Clear()
        {
            this.Nodes.Clear();
        }

        public bool ContainsNode(int tileIndex)
        {
            return this.Nodes.ContainsKey(tileIndex);
        }
    }
}
