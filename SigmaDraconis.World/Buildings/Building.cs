namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Config;
    using Language;
    using ProtoBuf;
    using Rooms;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    [ProtoInclude(1, typeof(LanderPanel))]
    [ProtoInclude(2, typeof(Lander))]
    [ProtoInclude(3, typeof(ConduitNode))]
    [ProtoInclude(4, typeof(ConduitMajor))]
    [ProtoInclude(5, typeof(Battery))]
    [ProtoInclude(7, typeof(FoodStorage))]
    [ProtoInclude(8, typeof(HydrogenStorage))]
    [ProtoInclude(9, typeof(Silo))]
    [ProtoInclude(10, typeof(SolarPanel))]
    [ProtoInclude(11, typeof(ItemsStorage))]
    [ProtoInclude(13, typeof(Rocket))]
    [ProtoInclude(14, typeof(WindTurbine))]
    [ProtoInclude(15, typeof(RocketGantry))]
    [ProtoInclude(17, typeof(FactoryBuilding))]
    [ProtoInclude(18, typeof(LaunchPad))]
    [ProtoInclude(19, typeof(GlassFactoryOld))]
    [ProtoInclude(21, typeof(DispenserBase))]
    [ProtoInclude(22, typeof(Wall))]
    [ProtoInclude(23, typeof(Door))]
    [ProtoInclude(24, typeof(Foundation))]
    [ProtoInclude(25, typeof(Roof))]
    [ProtoInclude(26, typeof(EnvironmentControl))]
    [ProtoInclude(30, typeof(Planter))]
    [ProtoInclude(32, typeof(Lab))]
    [ProtoInclude(33, typeof(SleepPod))]
    [ProtoInclude(34, typeof(LandingPod))]
    [ProtoInclude(35, typeof(Table))]
    [ProtoInclude(36, typeof(Lamp))]
    [ProtoInclude(37, typeof(DirectionalHeater))]
    [ProtoInclude(38, typeof(CompositesFactoryOld))]
    [ProtoInclude(39, typeof(SolarCellFactoryOld))]
    [ProtoInclude(40, typeof(BatteryCellFactoryOld))]
    [ProtoInclude(41, typeof(SoilSynthesiser))]
    [ProtoInclude(42, typeof(WaterStorage))]
    [ProtoInclude(43, typeof(OreScanner))]
    [ProtoInclude(44, typeof(ConduitMinor))]
    public class Building : Thing, IThingWithShadow, IBuildableThing
    {
        protected int animationFrame = 1;
        protected float definitionSoundVolume = 0f;
        protected float definitionSoundFade = 0.02f;

        [ProtoMember(101)]
        public int ConstructionProgress { get; set; }

        [ProtoMember(102)]
        public int AnimationFrame
        {
            get
            {
                return this.animationFrame;
            }
            set
            {
                if (this.animationFrame != value)
                {
                    if (this.mainTile != null) EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.AnimationFrame), this.animationFrame, value, this.mainTile.Row, this.ThingType);

                    this.animationFrame = value;
                    if (this.IsReady)
                    {
                        EventManager.RaiseEvent(EventType.Building, EventSubType.Updated, this);  // OBSOLETE
                    }
                }
            }
        }

        [ProtoMember(103)]
        protected double constructionProgressExact;

        [ProtoMember(104, IsRequired = true)]
        public bool IsReady { get; set; }

        [ProtoMember(105)]
        private Dictionary<int, int> colonistsByAccessTileForRepair;

        [ProtoMember(106)]
        private int underConstructionTimer = 0;

        [ProtoMember(107, IsRequired = true)]
        public bool IsConstructedFromPrefab { get; set; }

        public ShadowModel ShadowModel { get; protected set; } = new ShadowModel();

        public override bool CanWalk => (this.Definition == null || !this.Definition.TileBlockModel.In(TileBlockModel.Circle, TileBlockModel.SmallCircle, TileBlockModel.Square)) && (this.ThingType.IsFoundation() || this.ThingType.IsConduit() || this.ConstructionProgress == 100);
        public virtual int RecycleTime => 0;

        public bool IsConstructionPaused => this.underConstructionTimer == 0;

        protected StringsForMouseCursor? canRecycleReasonStringId = null;
        public string CanRecycleReason => this.canRecycleReasonStringId.HasValue ? LanguageManager.Get<StringsForMouseCursor>(this.canRecycleReasonStringId) : null;

        public Building() : base(ThingType.None)
        {
        }

        public Building(ThingType thingType) : base(thingType)
        {

        }

        public Building(ThingType thingType, ISmallTile mainTile, int size) : base(thingType, mainTile, size)
        {
        }

        public Building(ThingType thingType, ISmallTile mainTile, List<ISmallTile> allTiles) : base(thingType, mainTile, allTiles)
        {
        }

        public override void AfterAddedToWorld()
        {
            // Event is to hide resource overlay when resources are covered up
            if (this.MainTile.IsMineResourceVisible) EventManager.EnqueueWorldPropertyChangeEvent(this.MainTileIndex, nameof(ISmallTile.IsMineResourceVisible), this.MainTile.Row);
            this.UpdatePoleShadowModel();
            this.UpdateShadowModel();

            var definition = ThingTypeManager.GetDefinition(this.ThingType);
            if (definition.SoundFileName != null && definition.SoundVolume > 0)
            {
                EventManager.EnqueueSoundAddEvent(this.id, definition.SoundFileName);
                this.definitionSoundVolume = definition.SoundVolume;
                this.definitionSoundFade = definition.SoundFade;
            }

            base.AfterAddedToWorld();
        }

        public override void BeforeRemoveFromWorld()
        {
            var definition = ThingTypeManager.GetDefinition(this.ThingType);
            if (definition.SoundFileName != null && definition.SoundVolume > 0) EventManager.EnqueueSoundRemoveEvent(this.id);
            base.BeforeRemoveFromWorld();
        }

        public virtual int GetAnimationFrameForDeconstructOverlay()
        {
            return this.ThingType == ThingType.Door ? this.animationFrame : 1;
        }

        public virtual bool IncrementConstructionProgress(double amountPercent)
        {
            this.underConstructionTimer = 3;
            this.constructionProgressExact += amountPercent;
            this.RenderAlpha = (float)this.constructionProgressExact * 0.01f;
            this.ShadowAlpha = this.RenderAlpha;
            this.ConstructionProgress = (int)Math.Round(this.constructionProgressExact);
            if (this.constructionProgressExact > 99.999)
            {
                this.constructionProgressExact = 100;
                this.ConstructionProgress = 100;
                this.RenderAlpha = 1f;
                return true;
            }
            else if (amountPercent < 0 && this.constructionProgressExact < 0.0001)
            {
                this.constructionProgressExact = 0;
                this.ConstructionProgress = 0;
                this.CompleteDeconstruction();
                return true;
            }

            return false;
        }

        protected void AddVerticalCylinderShadowQuad(List<Vector3> model, float cx, float cy, float width, float height, float z1, float z2, int points, bool capped)
        {
            this.AddVerticalCylinderShadowQuad(model, cx, cy, width, width, height, height, z1, z2, points, capped);
        }

        protected void AddVerticalCylinderShadowQuad(List<Vector3> model, float cx, float cy, float baseWidth, float topWidth, float baseHeight, float topHeight, float z1, float z2, int points, bool capped)
        {
            float rad = 0 - (1f / points) * (float)Math.PI;
            float x1 = (float)Math.Sin(rad) * baseWidth;
            float x2 = (float)Math.Sin(rad) * topWidth;
            float y1 = (float)Math.Cos(rad) * baseHeight;
            float y2 = (float)Math.Cos(rad) * topHeight;
            this.AddVerticalShadowQuad(model, cx, cy, 0, baseHeight, x1, y1, z1, z2, x2 - x1, y2 - y1);
            if (capped)
            {
                model.Add(new Vector3(cx, cy + topHeight, z2));
                model.Add(new Vector3(cx + x2, cy + y2, z2));
                model.Add(new Vector3(cx, cy - topHeight, z2));
                model.Add(new Vector3(cx - x2, cy - y2, z2));
            }

            for (int i = 1; i <= points; ++i)
            {
                var prevX = x1;
                var prevY = y1;
                rad += (2f / points) * (float)Math.PI;
                x1 = (float)Math.Sin(rad) * baseWidth;
                x2 = (float)Math.Sin(rad) * topWidth;
                y1 = (float)Math.Cos(rad) * baseHeight;
                y2 = (float)Math.Cos(rad) * topHeight;

                this.AddVerticalShadowQuad(model, cx, cy, prevX, prevY, x1, y1, z1, z2, x2 - x1, y2 - y1);
                if (capped)
                {
                    // Should only have to do half of these, but doesn't work well for some reason
                    model.Add(new Vector3(cx + prevX, cy + prevY, z2));
                    model.Add(new Vector3(cx + x2, cy + y2, z2));
                    model.Add(new Vector3(cx - prevX, cy - prevY, z2));
                    model.Add(new Vector3(cx - x2, cy - y2, z2));
                }
            }
        }

        protected void AddHorizontalCylinderShadowQuad(List<Vector3> model, float cx, float cy, float x1, float y1, float x2, float y2, float cz, float radius, int points)
        {
            var step = 2f / points;

            for (var i = step - 1f; i <= 1.0001f; i += step)
            {
                var z1 = cz + (radius * (i - step));
                var z2 = cz + (radius * i);
                var wx1 = (float)Math.Cos((i - step) * 0.5f * Math.PI) * radius;
                var wy1 = wx1 * 0.5f;
                var wx2 = (float)Math.Cos(i * 0.5f * Math.PI) * radius;
                var wy2 = wx2 * 0.5f;

                model.Add(new Vector3(cx + x1 - wx2, cy + y1 - wy2, z2));
                model.Add(new Vector3(cx + x2 - wx2, cy + y2 - wy2, z2));
                model.Add(new Vector3(cx + x2 - wx1, cy + y2 - wy1, z1));
                model.Add(new Vector3(cx + x1 - wx1, cy + y1 - wy1, z1));

                model.Add(new Vector3(cx + x1 + wx2, cy + y1 + wy2, z2));
                model.Add(new Vector3(cx + x2 + wx2, cy + y2 + wy2, z2));
                model.Add(new Vector3(cx + x2 + wx1, cy + y2 + wy1, z1));
                model.Add(new Vector3(cx + x1 + wx1, cy + y1 + wy1, z1));
            }
        }

        protected void AddHorizontalShadowQuad(List<Vector3> model, float cx, float cy, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4, float z)
        {
            model.Add(new Vector3(cx + x1, cy + y1, z));
            model.Add(new Vector3(cx + x2, cy + y2, z));
            model.Add(new Vector3(cx + x3, cy + y3, z));
            model.Add(new Vector3(cx + x4, cy + y4, z));
        }

        protected void AddVerticalShadowQuad(List<Vector3> model, float cx, float cy, float x1, float y1, float x2, float y2, float z1, float z2, float topOffsetX = 0, float topOffsetY = 0)
        {
            model.Add(new Vector3(cx + x1, cy + y1, z1));
            model.Add(new Vector3(cx + x2, cy + y2, z1));
            model.Add(new Vector3(cx + x2 + topOffsetX, cy + y2 + topOffsetY, z2));
            model.Add(new Vector3(cx + x1 + topOffsetX, cy + y1 + topOffsetY, z2));
        }

        protected void AddShadowQuad(List<Vector3> model, float cx, float cy, float x1, float y1, float z1, float x2, float y2, float z2, float x3, float y3, float z3, float x4, float y4, float z4)
        {
            model.Add(new Vector3(cx + x1, cy + y1, z1));
            model.Add(new Vector3(cx + x2, cy + y2, z2));
            model.Add(new Vector3(cx + x3, cy + y3, z3));
            model.Add(new Vector3(cx + x4, cy + y4, z4));
        }

        // Call when construction complete
        public virtual void AfterConstructionComplete()
        {
            this.IsReady = true;
            foreach (var tile in this.AllTiles) tile.UpdatePathFinderNode();
            this.canRecycleReasonStringId = StringsForMouseCursor.InUse;
            EventManager.RaiseEvent(EventType.Building, EventSubType.Ready, this);
        }

        protected virtual void OnTimer(object sender)
        {
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var result = this.Definition.ConstructionCosts.ToDictionary(kv => kv.Key, kv => kv.Value);
            result.Add(ItemType.Biomass, 0);
            result.Add(ItemType.IronOre, 0);
            return result;
        }

        public virtual void UpdateShadowModel()
        {
        }

        public virtual void UpdatePoleShadowModel()
        {
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{this.ThingTypeStr}_{this.animationFrame}";
        }

        public override string ToString()
        {
            return $"{this.ShortName} {this.Id}";
        }

        private List<Vector3> poleShadowModel = new List<Vector3>();
        private List<float> poleShadowWidthFactors = new List<float>();
        public bool HasPoleShadowModel => this.poleShadowModel.Any();

        public List<Vector3> GetPoleShadowModel()
        {
            return this.poleShadowModel;
        }

        public List<float> GetPoleShadowModelWidthFactors()
        {
            return this.poleShadowWidthFactors;
        }

        protected void SetPoleShadowModel(List<Vector3> model, List<float> widthFactors)
        {
            this.poleShadowModel = model.ToList();
            this.poleShadowWidthFactors = widthFactors.ToList();
        }

        public virtual bool CanRecycle()
        {
            return !this.IsRecycling && this.definition?.CanRecycle == true;
        }

        public virtual void Recycle()
        {
            if (!this.IsReady && this.ConstructionProgress == 0 && this.RecycleProgress == 0)
            {
                // Construct not yet commited, can just remove.
                this.CompleteDeconstruction(true);
            }
            else
            {
                this.IsDesignatedForRecycling = true;
            }
        }

        public virtual void CancelRecycle()
        {
            this.IsDesignatedForRecycling = false;
        }

        public override void Update()
        {
            if (this.IsDesignatedForRecycling)
            {
                this.IsReady = false;
                this.IsRecycling = true;
                if (this.constructionProgressExact > 0)
                {
                    // Deconstruct is twice as fast as construct
                    this.IncrementConstructionProgress(-100f / (this.Definition.ConstructionTimeMinutes * 30f));
                }
                else
                {
                    this.CompleteDeconstruction();
                }
            }

            if (this.underConstructionTimer > 0) this.underConstructionTimer--;

            base.Update();
        }

        protected virtual void CompleteDeconstruction(bool refundEnergy = false)
        {
            this.IsDesignatedForRecycling = false;

            if (this.IsConstructedFromPrefab && refundEnergy)
            {
                World.Prefabs.Add(this.ThingType);
            }
            else
            {
                if (World.GetThings(ThingType.Lander).FirstOrDefault() is ILander lander)
                {
                    var network = World.ResourceNetwork;
                    if (network != null)
                    {
                        var resources = this.definition.ConstructionCosts;
                        foreach (var kv in resources.Where(x => x.Value > 0))
                        {
                            for (int i = 0; i < kv.Value; i++)
                            {
                                if (network.CanAddItem(kv.Key))
                                {
                                    network.AddItem(kv.Key);
                                    if (kv.Key == ItemType.Metal) WorldStats.Increment(WorldStatKeys.MetalUsed, -1);
                                    else if (kv.Key == ItemType.Stone) WorldStats.Increment(WorldStatKeys.StoneUsed, -1);
                                }
                                else i = kv.Value;
                            }
                        }

                        if (refundEnergy)
                        {
                            var energy = this.Definition.EnergyCost;
                            if (energy > 0)
                            {
                                network.AddEnergy(energy);
                                WorldStats.Increment(WorldStatKeys.EnergyUsed, 0 - energy.Joules);
                            }
                        }
                    }
                }
            }

            // Remove any blueprints
            var b = World.ConfirmedBlueprints.Values.FirstOrDefault(v => v.MainTile == this.MainTile && v.ThingType == this.ThingType 
                && (!(this is IRotatableThing) || v.Direction == (this as IRotatableThing).Direction));
            if (b != null)
            {
                World.ConfirmedBlueprints.Remove(b.Id);
                EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Removed, b);
            }

            // If it's a wall or roof with a different room on both sides, then merge the rooms
            if (this is IWall wall) RoomManager.MergeRooms(wall);

            // Remove any conduit
            var canRemoveConduit = true;
            List<IBuildableThing> conduits = new List<IBuildableThing>();
            foreach (var building in this.AllTiles.SelectMany(t => t.ThingsPrimary).OfType<IBuildableThing>())
            {
                if (building == this) continue;
                if (this.ThingType != ThingType.ConduitNode && building.ThingType == ThingType.ConduitNode) continue;
                if (building.ThingType == ThingType.ConduitMajor || building.ThingType == ThingType.ConduitMinor)
                {
                    if (building.ThingType == this.definition.ConduitType) conduits.Add(building);
                    continue;
                }

                canRemoveConduit = false;
                break;
            }

            if (canRemoveConduit)
            {
                foreach (var conduit in conduits) World.RemoveThing(conduit);

                // Also remove any conduit blueprint
                foreach (var conduitBlueprint in World.ConfirmedBlueprints.Values.Where(bp => this.AllTiles.Contains(bp.MainTile) && bp.ThingType.In(ThingType.ConduitMinor, ThingType.ConduitMajor)).ToList())
                {
                    World.ConfirmedBlueprints.Remove(conduitBlueprint.Id);
                    EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Removed, conduitBlueprint);
                }
            }

            World.RemoveThing(this);
        }

        public virtual bool DoRepair(double workSpeed)
        {
            if (this is IRepairableThing repairable)
            {
                if (workSpeed < 0.1) workSpeed = 0.1;
                repairable.MaintenanceLevel += workSpeed * 2.0 / (60.0 * this.Definition.ConstructionTimeMinutes);
                if (repairable.MaintenanceLevel >= 1.0)
                {
                    repairable.MaintenanceLevel = 1.0;
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<ISmallTile> GetAllAccessTilesForRepair()
        {
            if (this.colonistsByAccessTileForRepair == null) this.colonistsByAccessTileForRepair = new Dictionary<int, int>();
            foreach (var t in this.allTiles)
            {
                for (int i = 4; i <= 7; i++)   // NE, SE, SW, NW
                {
                    var direction = (Direction)i;
                    var tile = t.GetTileToDirection(direction);
                    if (tile == null || !tile.CanWorkInTile || t.HasWallToDirection(direction)) continue;   // Can't work here
                    //if (direction == Direction.N && (this.MainTile.HasWallToDirection(Direction.NW) && this.MainTile.HasWallToDirection(Direction.NE))) continue;
                    //if (direction == Direction.E && (this.MainTile.HasWallToDirection(Direction.NE) && this.MainTile.HasWallToDirection(Direction.SE))) continue;
                    //if (direction == Direction.S && (this.MainTile.HasWallToDirection(Direction.SE) && this.MainTile.HasWallToDirection(Direction.SW))) continue;
                    //if (direction == Direction.W && (this.MainTile.HasWallToDirection(Direction.SW) && this.MainTile.HasWallToDirection(Direction.NW))) continue;
                    yield return tile;
                }

                for (int i = 0; i <= 3; i++)   // E, W, S, N
                {
                    var direction = (Direction)i;
                    var tile = t.GetTileToDirection(direction);
                    if (tile == null || !tile.CanWorkInTile || t.HasWallToDirection(direction)) continue;   // Can't work here
                    //if (direction == Direction.N && (this.MainTile.HasWallToDirection(Direction.NW) && this.MainTile.HasWallToDirection(Direction.NE))) continue;
                    //if (direction == Direction.E && (this.MainTile.HasWallToDirection(Direction.NE) && this.MainTile.HasWallToDirection(Direction.SE))) continue;
                    //if (direction == Direction.S && (this.MainTile.HasWallToDirection(Direction.SE) && this.MainTile.HasWallToDirection(Direction.SW))) continue;
                    //if (direction == Direction.W && (this.MainTile.HasWallToDirection(Direction.SW) && this.MainTile.HasWallToDirection(Direction.NW))) continue;
                    yield return tile;
                }
            }
        }

        public IEnumerable<ISmallTile> GetAccessTilesForRepair(int? colonistId = null)
        {
            this.CleanupColonistAssignmentsForRepair();
            if (colonistId.HasValue && this.colonistsByAccessTileForRepair.Values.Any(v => v != colonistId.Value)) yield break;  // Assigned to someone else

            foreach (var tile in this.GetAllAccessTilesForRepair())
            {
                if (tile.ThingsPrimary.Any(t => t is IColonist c && (colonistId == null || c.Id != colonistId) && !c.IsMoving && !c.IsRelaxing)) continue;   // Blocked by another colonist
                yield return tile;
            }
        }

        public bool CanAssignColonistForRepair(int colonistId, int? tileIndex = null)
        {
            if (this.colonistsByAccessTileForRepair == null) this.colonistsByAccessTileForRepair = new Dictionary<int, int>();
            return tileIndex.HasValue
                ? this.GetAccessTilesForRepair(colonistId).Any(t => t.Index == tileIndex.Value)
                : this.GetAccessTilesForRepair(colonistId).Any();
        }

        public void AssignColonistForRepair(int colonistId, int tileIndex)
        {
            if (this.colonistsByAccessTileForRepair == null) this.colonistsByAccessTileForRepair = new Dictionary<int, int>();
            if (!this.colonistsByAccessTileForRepair.ContainsKey(tileIndex)) this.colonistsByAccessTileForRepair.Add(tileIndex, colonistId);
            else this.colonistsByAccessTileForRepair[tileIndex] = colonistId;
        }

        private void CleanupColonistAssignmentsForRepair()
        {
            if (this.colonistsByAccessTileForRepair == null) this.colonistsByAccessTileForRepair = new Dictionary<int, int>();
            foreach (var id in this.colonistsByAccessTileForRepair.Keys.ToList())
            {
                if (World.GetThing(this.colonistsByAccessTileForRepair[id]) is IColonist c && c.ActivityType == ColonistActivityType.Repair && c.TargetBuilingID == this.Id && !c.IsDead) continue;
                this.colonistsByAccessTileForRepair.Remove(id);
            }
        }
    }
}
