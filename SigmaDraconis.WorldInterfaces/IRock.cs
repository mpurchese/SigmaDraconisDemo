namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IRock : IRecyclableThing, IColonistInteractive
    {
        ItemType ResourceType { get; }
    }
}
