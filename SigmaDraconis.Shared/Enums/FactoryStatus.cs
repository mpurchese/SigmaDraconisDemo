namespace SigmaDraconis.Shared
{
    public enum FactoryStatus
    {
        Offline,
        Initialising,
        Starting,
        Standby,
        InProgress,
        WaitingToDistribute,
        NoPower,
        Broken,

        Pausing,
        Paused,
        Resuming,
        Stopping,

        // Cooker
        Opening,
        Open,
        Closing,

        // Resource processor
        InProgressReverse,

        // Mine
        NoResource,

        // Mush factory
        TooCold,

        // Water pump
        TooDry,

        // Ore Scanner
        ScanComplete
    }
}
