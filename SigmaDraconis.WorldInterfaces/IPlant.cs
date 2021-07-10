namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;

    public interface IPlant : IRecyclableThing, IColonistInteractive
    {
        int GrowthStage { get; }
        int MaxGrowthStage { get; }
        bool CanFlower { get; }
        bool IsFlowering { get; }
        bool HasDeadFrame { get; }
        bool IsDead { get; }
        long NextGrowthUpdateFrame { get; set; }

        List<int> UpdateGrowth();
    }
}
