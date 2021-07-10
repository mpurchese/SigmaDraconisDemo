namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;
    using Draconis.Shared;
    using Shared;

    public interface ISmallTile
    {
        IBigTile BigTile { get; set; }

        int X { get; }
        int Y { get; }
        int Row { get; }
        int Index { get; }

        TerrainType TerrainType { get; set; }

        Vector2f CentrePosition { get; }
        Vector2i TerrainPosition { get; }

        List<IThing> ThingsPrimary { get; }   // Things for which this is the main tile
        List<IThing> ThingsAll { get; }

        // Resources for mining.
        bool IsMineResourceVisible { get; }
        int MineResourceCount { get; }
        MineResourceDensity MineResourceDensity { get; }
        ItemType MineResourceType { get; }
        double MineResourceExtrationProgress { get; }
        double MineResourceSurveyProgress { get; }
        long OreScannerLstFrame { get; set; }
        int? MineResourceMineId { get; }
        int? MineResourceSurveyReservedBy { get; }
        long MineResourceSurveyReservedAt { get; }
        IMineTileResource GetResources();
        void SetResources(IMineTileResource resource);
        void SetResourceExtractionProgress(double progress);
        void ReserveForResourceSurvey(int colonistId);
        bool InrementResourceSurveyProgress(double progress);
        void RemoveResource(int count = 1);
        void SetIsMineResourceVisible(bool value = true, bool raiseEvent = true);
        void SetMineResourceMineId(int? mineId);

        // Soil types control plant growth areas
        BiomeType BiomeType { get; set; }

        // Ground cover
        int GroundCoverDensity { get; set; }
        int GroundCoverMaxDensity { get; set; }
        Direction GroundCoverDirection { get; set; }

        void UpdatePathFinderNode();
        bool UpdateIsCorridor();

        ISmallTile TileToN { get; }
        ISmallTile TileToNE { get; }
        ISmallTile TileToE { get; }
        ISmallTile TileToSE { get; }
        ISmallTile TileToS { get; }
        ISmallTile TileToSW { get; }
        ISmallTile TileToW { get; }
        ISmallTile TileToNW { get; }

        List<ISmallTile> AdjacentTiles4 { get; }
        List<ISmallTile> AdjacentTiles8 { get; }

        Dictionary<int, HeatOrLightSource> LightSources { get; }
        Dictionary<int, HeatOrLightSource> HeatSources { get; }

        ISmallTile GetTileToDirection(Direction direction);
        bool HasWallToDirection(Direction direction);
        bool HasWallOrDoorToDirection(Direction direction);

        void LinkTiles(ISmallTile n, ISmallTile ne, ISmallTile e, ISmallTile se, ISmallTile s, ISmallTile sw, ISmallTile w, ISmallTile nw);

        void RemoveThing(IThing thing);

        bool CanWalk { get; }
        bool CanWorkInTile { get; }
        bool CanPickupFromTile { get; }
        bool IsCorridor { get; }

        int WindModifier { get; set; }
    }
}
