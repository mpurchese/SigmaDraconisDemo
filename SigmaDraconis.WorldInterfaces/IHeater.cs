namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IHeater : IBuildableThing, IEnergyConsumer
    {
        bool IsOn { get;set; }
        bool IsAutomatic { get; set; }
        RoomTemperatureSetting HeaterSetting { get; set; }  // Obsolete
        int TargetTemperature { get; set; }
        bool IsIndoorMode { get; set; }
        void UpdateHeater(out Energy energyUsed);
    }
}