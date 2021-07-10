namespace SigmaDraconis.Shared
{
    public enum BuildingDisplayStatus
    {
        None, Offline, Initialising, Online, InProgress, InProgressPercent, NoPower, Waiting, Stopping, Preparing, Paused, Pausing, Resuming, Ready, Starting, Standby, SilosFull, StorageFull
            , NoResource, NoFood, NoWater, NotEnoughWater, Broken, InUse, Emptying, Filling, MiningUnknown, MiningOre, MiningCoal, MiningStone, TooCold, TooHot, TooDark, TooDry
            , SelectCrop, WaitingForSeeds, WaitingToHarvest, Dead, ScanComplete
    }
}
