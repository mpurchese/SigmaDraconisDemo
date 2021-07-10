namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;
    using Shared;

    public interface IMine : IEnergyConsumer, IResourceProviderBuilding, IRepairableThing, IThingWithTileHighlights
    {
        bool[] TileSelections { get; }

        ItemType CurrentResource { get; }

        int CurrentTileIndex { get; }
        bool IsMiningUnknownResource { get; }
        bool IsMineExhausted { get; }

        Dictionary<ItemType, int> RemainingResources { get; }
    }
}
