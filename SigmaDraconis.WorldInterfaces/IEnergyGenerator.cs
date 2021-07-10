namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IEnergyGenerator : IThing
    {
        Energy UpdateGenerator();

        Energy EnergyGenRate { get; }
    }
}
