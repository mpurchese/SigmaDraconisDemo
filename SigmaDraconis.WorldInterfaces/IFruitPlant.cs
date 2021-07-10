namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IFruitPlant : IAnimalInteractive, IColonistInteractive
    {
        int CountFruitAvailable { get; }
        WorkPriority HarvestFruitPriority { get; }
        float? HarvestJobProgress { get; }

        bool CanFruit { get; }
        bool CanFruitUnripe { get; }
        bool HasFruitUnripe { get; }

        bool DoHarvestJob(double workSpeed);
        void RemoveFruit(bool leaveSeed);
        void SetHarvestFruitPriority(WorkPriority value);
    }
}
