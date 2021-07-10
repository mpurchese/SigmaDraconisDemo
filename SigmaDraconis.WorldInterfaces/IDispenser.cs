namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IDispenser : IColonistInteractive
    {
        float DispenserProgress { get; set; }
        DispenserStatus DispenserStatus { get; }
        bool IsDispenserSwitchedOn { get; set; }
        void UpdateDispenser();
        int CountColonistAssignments(int? excludingColonistId = null);
    }
}