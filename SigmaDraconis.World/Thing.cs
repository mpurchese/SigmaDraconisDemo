namespace SigmaDraconis.World
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ProtoBuf;

    using Draconis.Shared;

    using Config;
    using Language;
    using Shared;

    using Blueprints;
    using Buildings;
    using Fauna;
    using Flora;
    using ResourceStacks;
    using Rocks;
    using Rooms;
    using WorldInterfaces;

    [ProtoContract]
    [ProtoInclude(1, typeof(Plant))]
    [ProtoInclude(3, typeof(Building))]
    [ProtoInclude(4, typeof(Blueprint))]
    [ProtoInclude(6, typeof(Animal))]
    [ProtoInclude(7, typeof(Rock))]
    [ProtoInclude(8, typeof(ResourceStack))]
    [ProtoInclude(9, typeof(StackingArea))]
    public abstract class Thing : IThing
    {
        protected static int nextId = 0;
        protected ISmallTile mainTile;
        protected List<ISmallTile> allTiles;
        protected int id;
        protected float renderAlpha = 1f;
        protected float shadowAlpha = 1f;
        protected float roomLightLevel;
        protected bool isDesignatedForRecycling;
        protected bool affectsPathFinders = true;

        private string thingTypeStr = null;
        protected string ThingTypeStr
        {
            get 
            { 
                if (this.thingTypeStr == null && this.ThingType != ThingType.None) this.thingTypeStr = this.ThingType.ToString();
                return this.thingTypeStr;
            }
        }

        public IRoom Room => RoomManager.GetRoom(this.MainTileIndex);

        public Thing()
        {
        }

        public Thing(ThingType thingType)
        {
            this.ThingType = thingType;
        }

        public Thing(ThingType thingType, ISmallTile mainTile, int size, bool addToTile = true)
        {
            this.id = ++nextId;
            this.ThingType = thingType;

            this.SetPosition(mainTile, size, addToTile);

            this.RenderAlpha = 1f;
            this.ShadowAlpha = 1f;

            this.RecyclePriority = WorkPriority.Disabled;
        }

        public Thing(ThingType thingType, ISmallTile mainTile, List<ISmallTile> allTiles, bool addToTile = true)
        {
            this.id = ++nextId;
            this.ThingType = thingType;

            this.mainTile = mainTile;
            this.allTiles = allTiles.ToList();

            var affectedTiles = new List<ISmallTile>();
            if (addToTile)
            {
                this.mainTile.ThingsPrimary.Add(this);
                foreach (var tile in this.allTiles)
                {
                    tile.ThingsAll.Add(this);
                    if (this.affectsPathFinders) tile.UpdatePathFinderNode();
                    affectedTiles.AddIfNew(tile);
                    foreach (var t in tile.AdjacentTiles8) affectedTiles.AddIfNew(t);
                }
            }

            foreach (var tile in affectedTiles) tile.UpdateIsCorridor();

            this.RenderAlpha = 1f;
            this.ShadowAlpha = 1f;

            this.RecyclePriority = WorkPriority.Disabled;
        }

        [ProtoMember(100)]
        public int Id
        {
            get
            {
                return this.id;
            }
            protected set
            {
                this.id = value;
                if (nextId <= value)
                {
                    nextId = value + 1;
                }
            }
        }

        [ProtoMember(101, IsRequired = true)]
        public ThingType ThingType { get; protected set; }

        [ProtoMember(102)]
        protected int mainTileIndex;

        [ProtoMember(103)]
        private List<int> allTileIndexes;

        [ProtoMember(104, IsRequired = true)]
        public float RenderAlpha
        {
            get
            {
                return this.renderAlpha;
            }
            set
            {
                if (this.renderAlpha != value)
                {
                    if (this.mainTile != null) EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.RenderAlpha), this.renderAlpha, value, this.mainTile.Row, this.ThingType);
                    this.renderAlpha = value;
                }
            }
        }

        [ProtoMember(105)]
        private bool isRecycling;
        public bool IsRecycling
        { get { return this.isRecycling; }
            set { this.isRecycling = value;
                if (value) this.BeforeRecycle();
            }
        }

        [ProtoMember(106)]
        public int RecycleProgress { get; set; }

        [ProtoMember(107, IsRequired = true)]
        public float ShadowAlpha
        {
            get
            {
                return this.shadowAlpha;
            }
            set
            {
                if (this.shadowAlpha != value)
                {
                    if (this.mainTile != null) EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.ShadowAlpha), this.shadowAlpha, value, this.mainTile.Row, this.ThingType);
                    this.shadowAlpha = value;
                }
            }
        }

        [ProtoMember(108)]
        public WorkPriority RecyclePriority { get; set; }

        [ProtoMember(109)]
        public bool IsDesignatedForRecycling
        {
            get
            {
                return this.isDesignatedForRecycling;
            }
            protected set
            {
                if (this.isDesignatedForRecycling != value)
                {
                    if (this.mainTile != null) EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.IsDesignatedForRecycling), this.isDesignatedForRecycling, value, this.mainTile.Row, this.ThingType);
                    this.isDesignatedForRecycling = value;
                }
            }
        }

        public virtual TileBlockModel TileBlockModel => this.Definition?.TileBlockModel ?? TileBlockModel.None;
        public virtual bool CanWalk => !this.TileBlockModel.In(TileBlockModel.Circle, TileBlockModel.SmallCircle, TileBlockModel.Square);
        public int MainTileIndex => this.MainTile.Index;

        protected ThingTypeDefinition definition = null;
        public ThingTypeDefinition Definition
        {
            get
            {
                if (this.definition == null) this.definition = ThingTypeManager.GetDefinition(this.ThingType, false);
                return this.definition;
            }
        }

        public string DisplayName => LanguageManager.GetName(this.ThingType);
        public string DisplayNameLower => LanguageManager.GetNameLower(this.ThingType);
        public string ShortName => LanguageManager.GetShortName(this.ThingType);
        public string ShortNameLower => LanguageManager.GetShortNameLower(this.ThingType);
        public string Description => LanguageManager.GetDescription(this.ThingType);

        public virtual void BeforeSerialization()
        {
            this.mainTileIndex = this.MainTile.Index;
            this.allTileIndexes = new List<int>();
            if (this.allTiles?.Any() == true)
            {
                this.allTileIndexes = allTiles.Select(t => t.Index).ToList();  // For serialization
            }
        }

        public virtual void AfterDeserialization()
        {
            this.mainTile = World.GetSmallTile(this.mainTileIndex);
            if (this.allTileIndexes?.Count > 0)
            {
                this.allTiles = this.allTileIndexes.Select(t => World.GetSmallTile(t)).OfType<ISmallTile>().ToList();
            }

            var affectedTiles = new List<ISmallTile>();
            if (!(this is Blueprint) && this.allTiles != null)
            {
                this.mainTile.ThingsPrimary.Add(this);
                foreach (var tile in this.allTiles)
                {
                    tile.ThingsAll.Add(this);
                    if (this.affectsPathFinders) tile.UpdatePathFinderNode();
                    affectedTiles.AddIfNew(tile);
                    foreach (var t in tile.AdjacentTiles8) affectedTiles.AddIfNew(t);
                }
            }

            foreach (var tile in affectedTiles) tile.UpdateIsCorridor();

            // Needed for old saves, this can be removed later
            if (this.ShadowAlpha < this.RenderAlpha)
            {
                this.ShadowAlpha = this.RenderAlpha;
            }
        }

        protected virtual void BeforeRecycle()
        {
        }

        public ISmallTile MainTile
        {
            get
            {
                return this.mainTile;
            }
        }

        public IReadOnlyList<ISmallTile> AllTiles
        {
            get
            {
                return this.allTiles?.AsReadOnly();
            }
        }

        public virtual void BeforeRemoveFromWorld() { }

        public virtual void AfterRemoveFromWorld() { }

        public virtual void AfterAddedToWorld() { }

        public void SetPosition(ISmallTile tile, int size, bool addToTile = true)
        {
            if (this.mainTile == tile) return;

            if (tile.TerrainType == TerrainType.DeepWater && !(this is IWaterAnimal) && !(this is IBird))
            {
                return;
            }

            if (size != 1 && size != 2 && size != 3 && size != 5) throw new ArgumentException("Unsupported thing size");

            var affectedTiles = new List<ISmallTile>();
            var updateCorridorTiles = !(this is IMoveableThing);

            if (addToTile)
            {
                if (this.mainTile != null && this.mainTile.ThingsPrimary.Contains(this)) this.mainTile.ThingsPrimary.Remove(this);
                if (this.allTiles != null)
                {
                    foreach (var t in this.allTiles)
                    {
                        if (t != null && t.ThingsAll.Contains(this))
                        {
                            t.ThingsAll.Remove(this);
                            if (this.affectsPathFinders) t.UpdatePathFinderNode();
                        }

                        if (updateCorridorTiles)
                        {
                            affectedTiles.AddIfNew(t);
                            foreach (var t2 in t.AdjacentTiles8) affectedTiles.AddIfNew(t2);
                        }
                    }
                }
            }

            this.mainTile = tile;

            if (size == 1)
            {
                this.allTiles = new List<ISmallTile> { mainTile };
            }
            else if (size == 2)
            {
                this.allTiles = new List<ISmallTile> { mainTile, mainTile.TileToNE, mainTile.TileToE, mainTile.TileToSE };
            }
            else
            {
                this.allTiles = new List<ISmallTile>(size * size);
                var range = (size - 1) / 2;
                for (int x = mainTile.X - range; x <= mainTile.X + range; x++)
                {
                    for (int y = mainTile.Y - range; y <= mainTile.Y + range; y++)
                    {
                        var t = World.GetSmallTile(x, y);
                        this.allTiles.Add(t);
                    }
                }
            }

            if (addToTile)
            {
                this.mainTile.ThingsPrimary.Add(this);
                foreach (var t in this.allTiles)
                {
                    t.ThingsAll.Add(this);
                    if (this.affectsPathFinders) t.UpdatePathFinderNode();
                    if (updateCorridorTiles)
                    {
                        affectedTiles.AddIfNew(t);
                        foreach (var t2 in t.AdjacentTiles8) affectedTiles.AddIfNew(t2);
                    }
                }
            }

            foreach (var t in affectedTiles) t.UpdateIsCorridor();
        }

        public void SetPosition(ISmallTile mainTile)
        {
            this.SetPosition(mainTile, new List<ISmallTile>(1) { mainTile });
        }

        public void SetPosition(ISmallTile mainTile, List<ISmallTile> allTiles)
        {
            if (mainTile.TerrainType == TerrainType.DeepWater && !(this is IWaterAnimal) && !(this is IBird))
            {
                return;
            }

            var affectedTiles = new List<ISmallTile>();
            var updateCorridorTiles = !(this is IMoveableThing);

            if (this.allTiles != null)
            {
                foreach (var t in this.allTiles)
                {
                    if (t != null)
                    {
                        t.RemoveThing(this);
                        if (updateCorridorTiles)
                        {
                            affectedTiles.AddIfNew(t);
                            foreach (var t2 in t.AdjacentTiles8) affectedTiles.AddIfNew(t2);
                        }
                    }
                }
            }

            this.mainTile = mainTile;
            this.allTiles = allTiles.ToList();

            this.mainTile.ThingsPrimary.Add(this);
            foreach (var t in this.allTiles)
            {
                t.ThingsAll.Add(this);
                if (updateCorridorTiles)
                {
                    affectedTiles.AddIfNew(t);
                    foreach (var t2 in t.AdjacentTiles8) affectedTiles.AddIfNew(t2);
                }

                if ((this.TileBlockModel != TileBlockModel.None || !t.CanWalk) && this.affectsPathFinders) t.UpdatePathFinderNode();
            }

            foreach (var tile in affectedTiles) tile.UpdateIsCorridor();
        }

        public virtual void Update()
        {
        }

        public virtual string GetTextureName(int layer = 1)
        {
            return this.ThingTypeStr;
        }

        public virtual Dictionary<ItemType, int> GetDeconstructionYield()
        {
            return null;
        }

        public virtual int GetDeconstructionYield(ItemType resourceType)
        {
            return this.GetDeconstructionYield()[resourceType];
        }

        public virtual Vector2f GetWorldPosition()
        {
            return this.MainTile.CentrePosition;
        }

        public virtual void UpdateRoom()
        {
        }
    }
}
