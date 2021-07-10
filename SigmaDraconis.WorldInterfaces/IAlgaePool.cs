namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;

    public interface IAlgaePool : IResourceProviderBuilding
    {
        double GrowthRate { get; }

        Dictionary<string, int> GrowthRateModifiers { get; }

        bool IsTooHot { get; }
        bool IsTooCold { get; }
        bool IsTooDark { get; }

        bool AutoFill { get; set; }
        bool AutoHarvest { get; set; }
        float Light { get; }

        void Fill();
        void Drain();
        void Harvest();

        bool CanFill();
        bool CanDrain();
        bool CanHarvest();
    }
}
