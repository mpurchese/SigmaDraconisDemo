namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;

    public interface IColonistInteractive : IThing
    {
        IEnumerable<ISmallTile> GetAccessTiles(int? colonistId = null);
        IEnumerable<ISmallTile> GetAllAccessTiles();   // For SmallTile.IsCorridor
        bool RequiresAccessNow { get; }
        bool CanAssignColonist(int colonistId, int? tileIndex = null);
        void AssignColonist(int colonistId, int tileIndex);
    }
}
