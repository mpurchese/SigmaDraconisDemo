using SigmaDraconis.Shared;

namespace SigmaDraconis.WorldInterfaces
{
    public interface IResourceProviderBuilding : IFactoryBuilding
    {
        ItemType OutputItemType { get; }
        int OutputItemCount { get; }
        bool CanTakeOutput(ItemType itemType);
        void TakeOutput();
    }
}