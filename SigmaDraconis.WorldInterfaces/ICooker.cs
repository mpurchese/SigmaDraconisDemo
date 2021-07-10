using SigmaDraconis.Shared;

namespace SigmaDraconis.WorldInterfaces
{
    public interface ICooker : IColonistInteractive, IFactoryBuilding, IEnergyConsumer
    {
        bool IsReadyToCook { get; }
        bool Fill(int? cropType);
        void Open();
    }
}