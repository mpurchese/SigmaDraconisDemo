using SigmaDraconis.Shared;

namespace SigmaDraconis.WorldInterfaces
{
    public interface IResourceConsumerBuilding : IFactoryBuilding
    {
        ItemType InputItemType { get; }
        bool CanAddInput(ItemType itemType);
        void AddInput(ItemType itemType);
    }
}