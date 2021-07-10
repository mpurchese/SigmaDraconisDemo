namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IBattery : IThing
    {
        Energy ChargeLevel { get; set; }

        Energy ChargeCapacity { get; }
    }
}
