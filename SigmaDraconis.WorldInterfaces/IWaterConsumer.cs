namespace SigmaDraconis.WorldInterfaces
{
    public interface IWaterConsumer : IBuildableThing
    {
        int WaterUseRate { get; }
    }
}
