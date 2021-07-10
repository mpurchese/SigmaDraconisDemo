namespace SigmaDraconis.Shared
{
    public enum EventType
    {
        // Game events
        Game, GameExit,

        // World events
        Building, Plant, Thing, Shadow, Wind, Animal, Colonist, Zone, StackingArea, ResourceStack, Door,

        // Blueprint events
        Blueprint, VirtualBlueprint, RecycleBlueprint, HighlightedTiles,

        // Timer events
        Timer1Second,

        // Other
        RocketLaunchClick,
        RocketLaunchStart,
        RocketLaunched,
        ResourcesForDeconstruction,
        PlantsForHarvest,
        BuildableArea,
        VirtualBuildableArea
    }
}
