namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IMineTileResource
    {
        ItemType Type { get; set; }
        int Count { get; set; }
        MineResourceDensity Density { get; set; }
        bool IsVisible { get; set; }
        double ExtractionProgress { get; set; }
        int? MineId { get; set; }
        double SurveyProgress { get; set; }
        int? ReservedBy { get; set; }
        long ReservedAt { get; set; }
        IMineTileResource Clone();
    }
}
