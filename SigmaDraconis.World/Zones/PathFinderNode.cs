namespace SigmaDraconis.World.Zones
{
    using System.Collections.Generic;
    using Shared;
    using WorldInterfaces;

    public class PathFinderNode : IPathFinderNode
    {
        public int Index { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public float C { get; set; }
        public float G { get; set; }
        public float F { get; set; }
        public float H { get; set; }
        public bool IsOpened { get; set; }
        public bool IsClosed { get; set; }
        public IPathFinderNode Parent { get; set; }

        public IPathFinderNode LinkNE { get; set; }
        public IPathFinderNode LinkSE { get; set; }
        public IPathFinderNode LinkSW { get; set; }
        public IPathFinderNode LinkNW { get; set; }
        public IPathFinderNode LinkN { get; set; }
        public IPathFinderNode LinkE { get; set; }
        public IPathFinderNode LinkS { get; set; }
        public IPathFinderNode LinkW { get; set; }

        public int CostNE { get; set; } = -1;
        public int CostSE { get; set; } = -1;
        public int CostSW { get; set; } = -1;
        public int CostNW { get; set; } = -1;
        public int CostN { get; set; } = -1;
        public int CostE { get; set; } = -1;
        public int CostS { get; set; } = -1;
        public int CostW { get; set; } = -1;

        public PathFinderNode(int index, int x, int y)
        {
            this.Index = index;
            this.X = x;
            this.Y = y;
        }

        public IPathFinderNode GetLink(Direction direction)
        {
            switch (direction)
            {
                case Direction.N: return this.LinkN;
                case Direction.NE: return this.LinkNE;
                case Direction.E: return this.LinkE;
                case Direction.SE: return this.LinkSE;
                case Direction.S: return this.LinkS;
                case Direction.SW: return this.LinkSW;
                case Direction.W: return this.LinkW;
                case Direction.NW: return this.LinkNW;
            }

            return null;
        }

        public IEnumerable<IPathFinderNode> AllLinks
        {
            get
            {
                if (this.LinkN != null) yield return this.LinkN;
                if (this.LinkNE != null) yield return this.LinkNE;
                if (this.LinkE != null) yield return this.LinkE;
                if (this.LinkSE != null) yield return this.LinkSE;
                if (this.LinkS != null) yield return this.LinkS;
                if (this.LinkSW != null) yield return this.LinkSW;
                if (this.LinkW != null) yield return this.LinkW;
                if (this.LinkNW != null) yield return this.LinkNW;
            }
        }

        public void SetLink(Direction direction, IPathFinderNode node)
        {
            switch (direction)
            {
                case Direction.N: this.LinkN = node; this.CostN = node != null ? 141 : -1; break;
                case Direction.NE: this.LinkNE = node; this.CostNE = node != null ? 100 : -1; break;
                case Direction.E: this.LinkE = node; this.CostE = node != null ? 141 : -1; break;
                case Direction.SE: this.LinkSE = node; this.CostSE = node != null ? 100 : -1; break;
                case Direction.S: this.LinkS = node; this.CostS = node != null ? 141 : -1; break;
                case Direction.SW: this.LinkSW = node; this.CostSW = node != null ? 100 : -1; break;
                case Direction.W: this.LinkW = node; this.CostW = node != null ? 141 : -1; break;
                case Direction.NW: this.LinkNW = node; this.CostNW = node != null ? 100 : -1; break;
            }
        }

        public override string ToString()
        {
            return $"Pos ({this.X},{this.Y})";
        }
    }
}
