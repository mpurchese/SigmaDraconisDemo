namespace SigmaDraconis.World.PathFinding
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // https://www.nuget.org/packages/OptimizedPriorityQueue/
    using Priority_Queue;

    using Draconis.Shared;

    using Shared;
    using WorldInterfaces;

    /// <summary>
    /// Path finder uses A* finder algorithm.
    /// Adapted from https://github.com/qiao/PathFinding.js
    /// </summary>
    public class PathFinder
    {
        public static Path FindPath(int startIndex, int endIndex, Dictionary<int, IPathFinderNode> nodes, HashSet<int> tilesToAvoid = null)
        {
            if (nodes != null && (!nodes.ContainsKey(startIndex) || !nodes.ContainsKey(endIndex))) return null;

            var openList = new SimplePriorityQueue<IPathFinderNode>();
            var modifiedList = new List<IPathFinderNode>();

            var startNode = nodes[startIndex];
            var endNode = nodes[endIndex];

            startNode.IsOpened = true;
            openList.Enqueue(startNode, 0);
            modifiedList.Add(startNode);

            while (openList.Count > 0)
            {
                var node = openList.Dequeue();
                node.IsClosed = true;

                // if reached the end position, construct the path and return it
                if (node.X == endNode.X && node.Y == endNode.Y)
                {
                    var path = BuildResult(node);
                    ResetNodes(modifiedList);
                    return new Path(new Vector2i(startNode.X, startNode.Y), new Vector2i(endNode.X, endNode.Y), path);
                }

                // Get neigbours of the current node
                var neighbours = new List<IPathFinderNode>();
                var N = node.LinkN;
                var E = node.LinkE;
                var S = node.LinkS;
                var W = node.LinkW;
                var NE = node.LinkNE;
                var NW = node.LinkNW;
                var SE = node.LinkSE;
                var SW = node.LinkSW;

                if (node.CostN > 0) { N.C = node.CostN; neighbours.Add(N); }
                if (node.CostE > 0) { E.C = node.CostE; neighbours.Add(E); }
                if (node.CostS > 0) { S.C = node.CostS; neighbours.Add(S); }
                if (node.CostW > 0) { W.C = node.CostW; neighbours.Add(W); }
                if (node.CostNE > 0) { NE.C = node.CostNE; neighbours.Add(NE); }
                if (node.CostNW > 0) { NW.C = node.CostNW; neighbours.Add(NW); }
                if (node.CostSE > 0) { SE.C = node.CostSE; neighbours.Add(SE); }
                if (node.CostSW > 0) { SW.C = node.CostSW; neighbours.Add(SW); }

                foreach (var neighbour in neighbours)
                {
                    if (neighbour.IsClosed || tilesToAvoid?.Contains(neighbour.Index) == true) continue;

                    // Get the distance between current node and the neighbor
                    // and calculate the next g score
                    var ng = node.G + neighbour.C;

                    // Check if the neighbor has not been inspected yet, or
                    // can be reached with smaller cost from the current node
                    if (!neighbour.IsOpened || ng < neighbour.G)
                    {
                        neighbour.G = ng;
                        neighbour.H = neighbour.H == 0 ? GetOctileDistance(Math.Abs(neighbour.X - endNode.X), Math.Abs(neighbour.Y - endNode.Y)) : neighbour.H;
                        neighbour.F = neighbour.G + neighbour.H;
                        neighbour.Parent = node;
                        modifiedList.Add(neighbour);

                        if (!neighbour.IsOpened)
                        {
                            openList.Enqueue(neighbour, neighbour.F);
                            neighbour.IsOpened = true;
                        }
                        else
                        {
                            // The neighbor can be reached with smaller cost.
                            // Since its f value has been updated, we have to
                            // update its position in the open list
                            openList.UpdatePriority(neighbour, neighbour.F);
                        }
                    }
                }
            }

            ResetNodes(modifiedList);
            return null;  // Fail to find path
        }

        private static float GetOctileDistance(int x, int y)
        {
            return Math.Max(x, y) + 0.414f * Math.Min(x, y);
        }

        private static Stack<PathNode> BuildResult(IPathFinderNode node)
        {
            var path = new Stack<PathNode>();
            var direction = node.Parent != null ? DirectionHelper.GetDirectionFromAdjacentPositions(node.X, node.Y, node.Parent.X, node.Parent.Y) : Direction.None;
            var next = new PathNode(node.X, node.Y, Direction.None, direction);
            path.Push(next);

            while (node.Parent != null)
            {
                var prevDirection = direction;
                
                next = new PathNode(next.X, next.Y, DirectionHelper.Reverse(prevDirection), Direction.None);
                if (node.Parent.Y > next.Y)
                {
                    next.Y++;
                }
                else if (node.Parent.Y < next.Y)
                {
                    next.Y--;
                }

                if (node.Parent.X > next.X)
                {
                    next.X++;
                }
                else if (node.Parent.X < next.X)
                {
                    next.X--;
                }

                if (next.X == node.Parent.X && next.Y == node.Parent.Y)
                {
                    node = node.Parent;
                }

                direction = node.Parent != null ? DirectionHelper.GetDirectionFromAdjacentPositions(node.X, node.Y, node.Parent.X, node.Parent.Y) : Direction.None;
                next.ReverseDirection = direction;
                path.Push(next);
            };

            return path;
        }

        private static void ResetNodes(List<IPathFinderNode> nodes)
        {
            foreach (var node in nodes)
            {
                node.IsOpened = false;
                node.IsClosed = false;
                node.G = 0;
                node.F = 0;
                node.H = 0;
                node.C = -1;
                node.Parent = null;
            }
        }
    }
}
