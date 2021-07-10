namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IDirectionalHeater : IHeater, IRotatableThing
    {
        SmoothedEnergy SmoothedEnergyUseRate { get; }
        void SetDirection(Direction direction);
    }
}