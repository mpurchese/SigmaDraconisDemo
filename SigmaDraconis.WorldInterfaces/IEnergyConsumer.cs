namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IEnergyConsumer : IThing
    {
        Energy EnergyUseRate { get; }
    }
}
