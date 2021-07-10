namespace SigmaDraconis.WorldGenerator
{
    using Shared;

    public interface IThingTemplate
    {
        ThingType ThingType { get; }
        SmallTileTemplate MainTile { get; }
    }
}
