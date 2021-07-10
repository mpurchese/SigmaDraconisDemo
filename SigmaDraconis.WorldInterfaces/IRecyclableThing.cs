namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;
    using Shared;

    public interface IRecyclableThing : IThing
    {
        int RecycleTime { get; }
        int RecycleProgress { get; set; }
        string CanRecycleReason { get; }
        bool CanRecycle();
        Dictionary<ItemType, int> GetDeconstructionYield();
        int GetDeconstructionYield(ItemType resourceType);
    }
}
