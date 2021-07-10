namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;
    using Shared;

    public interface IRepairableThing : IThing
    {
        double MaintenanceLevel { get; set; }
        WorkPriority RepairPriority { get; set; }
        bool DoRepair(double workSpeed);

        IEnumerable<ISmallTile> GetAccessTilesForRepair(int? colonistId = null);
        IEnumerable<ISmallTile> GetAllAccessTilesForRepair();   // For SmallTile.IsCorridor
        bool CanAssignColonistForRepair(int colonistId, int? tileIndex = null);
        void AssignColonistForRepair(int colonistId, int tileIndex);
    }
}
