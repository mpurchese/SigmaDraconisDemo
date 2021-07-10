namespace SigmaDraconis.WorldInterfaces
{
    public interface IPowerPlant : IEnergyGenerator, IRepairableThing, IFactoryBuilding, IResourceConsumerBuilding, IWaterConsumer
    {
        int BurnRateSetting { get; set; }
        double ConsumptionRate { get; }
        double BurnRateSettingToKW(int setting);
    }
}
