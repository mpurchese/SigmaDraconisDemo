namespace SigmaDraconis.WorldInterfaces
{
    public interface IWaterPump : IWaterProviderBuilding
    {
        int? RequestingWaterFromGroundTile { get; set; }
        int ExtractionRate { get; set; }
    }
}
