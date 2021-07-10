namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IEnvironmentControl : IBuildableThing, IEnergyConsumer
    {
        bool IsOn { get; }
        RoomLightSetting LightSetting { get; set; }
        RoomTemperatureSetting TemperatureSetting { get; set; }
        int TargetTempMin { get; set; }
        int TargetTempMax { get; set; }
        int FanAnimationFrame { get; }
        int ScreenAnimationFrame { get; }
        SmoothedEnergy SmoothedEnergyUseRate { get; }
        void TogglePower();
        void UpdateRoomControl(out Energy energyUsed);
    }
}