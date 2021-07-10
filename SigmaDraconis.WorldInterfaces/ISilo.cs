namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface ISilo : IThing
    {
        SiloStatus SiloStatus { get; set; }
        bool IsSiloSwitchedOn { get; set; }
        int StorageLevel { get; }
        int StorageCapacity { get; }
        int CountItems(ItemType itemType);
        bool CanAddItem(ItemType itemType);
        void AddItem(ItemType itemType);
        bool CanTakeItems(ItemType itemType, int count = 1);
        int TakeItems(ItemType itemType, int count = 1);
        bool SwapItem(ItemType itemTypeToRemove, ItemType itemTypeToAdd);
    }
}
