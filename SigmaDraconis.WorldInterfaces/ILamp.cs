namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface ILamp : IBuildableThing, IEnergyConsumer
    {
        bool IsOn { get; set; }
        bool IsAutomatic { get; set; }
        RoomLightSetting LightSetting { get; set; }  // Obsolete

        void UpdateLamp(out Energy energyUsed);
    }
}