namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface ILander : IBattery, ISilo, IEnergyGenerator, IBuildableThing, IConduitNode
    {
        ResourceContainer ItemsContainer { get; }
        ResourceContainer FoodContainer { get; }
    }
}