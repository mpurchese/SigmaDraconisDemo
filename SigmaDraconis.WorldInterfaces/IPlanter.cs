namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;
    using Shared;

    public interface IPlanter : IColonistJobProvider, IColonistInteractive, IBuildableThing, IWaterConsumer
    {
        int SelectedCropTypeId { get; }

        double? Progress { get; }

        float? JobProgress { get; }

        double? GrowthRate { get; }

        double? Health { get; }

        PlanterStatus PlanterStatus { get; }

        int CurrentCropTypeId { get; }

        bool IsTooHot { get; }

        bool IsTooCold { get; }

        bool IsTooDark { get; }

        WorkPriority FarmPriority { get; set; }

        bool RemoveCrop { get; }

        bool HasWater { get; }

        Dictionary<string, int> GrowthRateModifiers { get; }

        void SetCrop(int cropTypeId, bool replaceExisting = false);

        void UpdatePlanter();
    }
}
