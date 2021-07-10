namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;
    using Shared;

    public interface ITable : IColonistInteractive, IBuildableThing
    {
        List<int> GetOtherColonists(int colonistID);
        bool HasKekNE { get; }
        bool HasKekSE { get; }
        bool HasKekSW { get; }
        bool HasKekNW { get; }

        void AddKek(Direction direction);
        void RemoveKek(Direction direction);
    }
}
