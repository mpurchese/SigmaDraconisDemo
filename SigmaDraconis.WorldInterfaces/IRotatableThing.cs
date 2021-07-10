namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IRotatableThing : IThing
    {
        Direction Direction { get; }
    }
}
