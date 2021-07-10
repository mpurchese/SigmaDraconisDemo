namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;
    using Draconis.Shared;
    using Shared;
    using Config;

    public interface IThing
    {
        bool CanWalk { get; }
        ThingTypeDefinition Definition { get; }
        int Id { get; }
        int MainTileIndex { get; }
        float RenderAlpha { get; set; }
        float ShadowAlpha { get; set; }
        bool IsRecycling { get; set; }
        bool IsDesignatedForRecycling { get; }

        string DisplayName { get; }
        string DisplayNameLower { get; }
        string ShortName { get; }
        string ShortNameLower { get; }
        string Description { get; }

        WorkPriority RecyclePriority { get; set; }
        ThingType ThingType { get; }
        TileBlockModel TileBlockModel { get; }

        ISmallTile MainTile { get; }
        IReadOnlyList<ISmallTile> AllTiles { get; }

        void BeforeSerialization();
        void AfterDeserialization();

        void SetPosition(ISmallTile mainTile);
        void SetPosition(ISmallTile mainTile, List<ISmallTile> allTiles);
        string GetTextureName(int layer = 1);
        Vector2f GetWorldPosition();
        void Update();
        void UpdateRoom();
        IRoom Room { get; }
        void AfterAddedToWorld();
    }
}
