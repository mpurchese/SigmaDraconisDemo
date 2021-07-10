namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IOreScanner : IEnergyConsumer, IAutoRestartable, IAnimatedThing, IThingWithTileHighlights
    {
        FactoryStatus FactoryStatus { get; }
        bool IsSwitchedOn { get; }
        double Progress { get; }
        int CurrentRadius { get; }
        int CurrentTileCount { get; }
        int TimeRemainingFrames { get; }
        void TogglePower();
        void UpdateScanner(out Energy energyUsed);
    }
}
