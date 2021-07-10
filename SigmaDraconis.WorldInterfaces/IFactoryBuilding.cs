using SigmaDraconis.Shared;

namespace SigmaDraconis.WorldInterfaces
{
    public interface IFactoryBuilding : IBuildableThing, IAutoRestartable
    {
        double FactoryProgress { get; set; }
        FactoryStatus FactoryStatus { get; set; }
        bool IsSwitchedOn { get; }
        double Temperature { get; }
        int? InventoryTarget { get; }
        bool InventoryTargetShutdown { get; }
        int FramesRemaining { get; }
        bool GeneratorHasWater { get; }
        Energy UpdateFactory();
        void TogglePower();
        void SetInventoryTarget(int? targetValue, bool stopOnComplete);
    }
}