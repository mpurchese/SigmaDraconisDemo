using SigmaDraconis.Shared;

namespace SigmaDraconis.WorldInterfaces
{
    public interface IStackingArea : IThing
    {
        ItemType ItemType { get; set; }
        StackingAreaMode Mode { get; set; }
        int TargetStackSize { get; set; }
        WorkPriority WorkPriority { get; set; }
        void UpdateStack();
    }
}