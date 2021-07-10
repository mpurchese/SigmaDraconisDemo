namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;
    using Shared;

    public interface IPathFinderNode
    {
        int Index { get; }
        int X { get; set; }
        int Y { get; set; }
        float C { get; set; }
        float G { get; set; }
        float F { get; set; }
        float H { get; set; }
        bool IsOpened { get; set; }
        bool IsClosed { get; set; }

        IPathFinderNode Parent { get; set; }

        IPathFinderNode LinkNE { get; set; }
        IPathFinderNode LinkSE { get; set; }
        IPathFinderNode LinkSW { get; set; }
        IPathFinderNode LinkNW { get; set; }
        IPathFinderNode LinkN { get; set; }
        IPathFinderNode LinkE { get; set; }
        IPathFinderNode LinkS { get; set; }
        IPathFinderNode LinkW { get; set; }

        int CostNE { get; set; }
        int CostSE { get; set; }
        int CostSW { get; set; }
        int CostNW { get; set; }
        int CostN { get; set; }
        int CostE { get; set; }
        int CostS { get; set; }
        int CostW { get; set; }

        IPathFinderNode GetLink(Direction direction);
        IEnumerable<IPathFinderNode> AllLinks { get; }
        void SetLink(Direction direction, IPathFinderNode node);
    }
}
