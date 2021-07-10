namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Projects;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class Mine : FactoryBuilding, IMine
    {
        private bool animationPrevIsOn;
        private int animationTimer;

        [ProtoMember(1)]
        public int CurrentTileIndex { get; private set; }

        [ProtoMember(2)]
        public int CurrentTileDirection { get; private set; }

        public bool[] TileSelections { get; private set; }

        // Protobuf gets arrays wrong for some reason
        [ProtoMember(11)]
        public List<bool> TileSelectionsForSerialize
        {
            get => this.TileSelections?.ToList();
            set { this.TileSelections = value != null ? value.ToArray() : new bool[9]; }
        }

        [ProtoMember(20, IsRequired = true)]
        public bool IsMineExhausted { get; private set; }

        [ProtoMember(21, IsRequired = true)]
        public bool IsMiningUnknownResource { get; private set; }

        [ProtoMember(22)]
        public ItemType CurrentResource
        {
            get => this.producedItemType;
            set { this.producedItemType = value; }
        }

        public Dictionary<ItemType, int> RemainingResources { get; protected set; }

        public Mine() : base()
        {
        }

        public Mine(ISmallTile mainTile) : base(ThingType.Mine, mainTile, 1)
        {
        }

        protected override void Init()
        {
            this.framesToInitialise = (int)(3600 * Constants.MineEnergyStore / Constants.MineEnergyUse);
            this.framesToPauseResume = Constants.MineFramesToPauseResume;
            this.energyPerHour = Energy.FromKwH(Constants.MineEnergyUse);
            this.energyPerFrame = energyPerHour / Constants.FramesPerHour;
            this.capacitorSize = Constants.MineEnergyStore;
            this.RemainingResources = new Dictionary<ItemType, int>();
            base.Init();
        }

        public override void AfterAddedToWorld()
        {
            if (this.TileSelections == null) this.TileSelections = new bool[9];
            if (this.FactoryProgress > 0 && this.CurrentTileIndex >= 0) this.framesToProcess = GetFramesToProcess(World.SmallTiles[this.CurrentTileIndex], false);
            this.UpdateRemainingResources();
            base.AfterAddedToWorld();
        }

        public override void AfterConstructionComplete()
        {
            this.UpdateRemainingResources();

            for (int i = 0; i < 9; i++)
            {
                var tile = i < 8 ? this.mainTile.GetTileToDirection((Direction)i) : this.mainTile;
                if (tile == null) continue;

                if (tile.MineResourceMineId.GetValueOrDefault(this.Id) == this.Id && (!tile.IsMineResourceVisible || tile.MineResourceType != ItemType.None))
                {
                    this.TileSelections[i] = true;
                    tile.SetMineResourceMineId(this.Id);
                }
            }

            base.AfterConstructionComplete();
        }

        protected override void CompleteDeconstruction(bool refundEnergy = false)
        {
            if (this.MainTile.MineResourceMineId == this.Id) this.MainTile.SetMineResourceMineId(null);
            foreach (var tile in this.mainTile.AdjacentTiles8)
            {
                if (tile.MineResourceMineId == this.Id) tile.SetMineResourceMineId(null);
            }

            for (int i = 0; i < 9; i++) this.TileSelections[i] = false;

            base.CompleteDeconstruction(refundEnergy);
        }

        protected override void TryStart()
        {
            this.FactoryProgress = 0.0;
            this.producedItemType = ItemType.None;
            if (this.MaintenanceLevel < 0.0001)
            {
                this.FactoryStatus = FactoryStatus.Broken;
                return;
            }

            this.UpdateRemainingResources();
            if (this.IsMineExhausted)
            {
                this.FactoryStatus = FactoryStatus.NoResource;
                return;
            }

            ISmallTile bestTile = null;
            int bestTileDirection = 0;
            var bestTileDensity = -1;
            if (this.CapacitorCharge >= this.capacitorSize || World.ResourceNetwork?.CanTakeEnergy(Energy.FromKwH(this.capacitorSize - this.CapacitorCharge)) == true)
            {
                var hasResource = false;
                for (int i = 0; i < 9; i++)
                {
                    if (!this.TileSelections[i]) continue;

                    var tile = i < 8 ? this.mainTile.GetTileToDirection((Direction)i) : this.mainTile;
                    if (tile == null) continue;

                    hasResource |= tile.MineResourceCount > 0 || !tile.IsMineResourceVisible;

                    if (this.InventoryTarget.HasValue && !tile.IsMineResourceVisible) continue;

                    var resourceType = tile.MineResourceType;
                    if (resourceType != ItemType.None && this.InventoryTarget.HasValue && World.ResourceNetwork.GetItemTotal(resourceType) >= this.InventoryTarget.Value) continue;

                    if (tile.MineResourceExtrationProgress > 0)
                    {
                        bestTile = tile;
                        bestTileDirection = i;
                        break;
                    }

                    if (!tile.IsMineResourceVisible)
                    {
                        if (bestTile == null)
                        {
                            bestTileDensity = 0;
                            bestTile = tile;
                            bestTileDirection = i;
                        }
                    }
                    else if (tile.MineResourceDensity > (MineResourceDensity)bestTileDensity)
                    {
                        bestTileDensity = (int)tile.MineResourceDensity;
                        bestTile = tile;
                        bestTileDirection = i;
                    }
                }

                if (bestTile != null)
                {
                    this.FactoryStatus = FactoryStatus.InProgress;
                    this.producedItemType = bestTile.MineResourceType;
                    this.IsMiningUnknownResource = !bestTile.IsMineResourceVisible;
                    this.CurrentTileIndex = bestTile.Index;
                    this.CurrentTileDirection = bestTileDirection;
                    this.framesToProcess = GetFramesToProcess(bestTile, false);
                }
                else this.FactoryStatus = hasResource ? FactoryStatus.Standby : FactoryStatus.NoResource;
            }
            else this.FactoryStatus = FactoryStatus.NoPower;
        }

        protected override bool ShouldPause()
        {
            if (!this.IsSwitchedOn) return true;
            if (!this.InventoryTarget.HasValue) return false;
            if (World.ResourceNetwork == null) return false;

            for (int i = 0; i < 9; i++)
            {
                if (!this.TileSelections[i]) continue;

                var tile = i < 8 ? this.mainTile.GetTileToDirection((Direction)i) : this.mainTile;
                if (tile == null || !tile.IsMineResourceVisible) continue;

                var resourceType = tile.MineResourceType;
                if (resourceType != ItemType.None && World.ResourceNetwork.GetItemTotal(resourceType) < this.InventoryTarget.Value) return false;
            }

            return true;
        }

        private int GetFramesToProcess(ISmallTile tile, bool ignoreUnexplored)
        {
            var multiplier = 1.0;
            if (ProjectManager.GetDefinition(202)?.IsDone == true) multiplier = 0.8;
            else if (ProjectManager.GetDefinition(201)?.IsDone == true) multiplier = 0.9;

            if (this.IsMiningUnknownResource && !ignoreUnexplored) return (int)(Constants.MineFramesToProcessNoSurvey * multiplier);
            if (tile.MineResourceDensity == MineResourceDensity.VeryLow || tile.MineResourceDensity == MineResourceDensity.None) return (int)(Constants.MineFramesToProcessVeryLowDensity * multiplier);
            if (tile.MineResourceDensity == MineResourceDensity.Low) return (int)(Constants.MineFramesToProcessLowDensity * multiplier);
            if (tile.MineResourceDensity == MineResourceDensity.Medium) return (int)(Constants.MineFramesToProcessMediumDensity * multiplier);
            if (tile.MineResourceDensity == MineResourceDensity.High) return (int)(Constants.MineFramesToProcessHighDensity * multiplier);
            return (int)(Constants.MineFramesToProcessVeryHighDensity * multiplier);
        }

        protected override void CompleteProcessing()
        {
            // Reveal tile content
            var tile = World.GetSmallTile(this.CurrentTileIndex);
            if (tile != null)
            {
                if (tile.MineResourceCount > 0 && this.producedItemType != ItemType.None)
                {
                    switch(this.producedItemType)
                    {
                        case ItemType.Coal: WorldStats.Increment(WorldStatKeys.CoalMined); break;
                        case ItemType.IronOre: WorldStats.Increment(WorldStatKeys.OreMined); break;
                        case ItemType.Stone: WorldStats.Increment(WorldStatKeys.StoneMined); break;
                    }

                    tile.RemoveResource();
                    tile.SetIsMineResourceVisible();
                    if (tile.MineResourceCount == 0)
                    {
                        tile.SetMineResourceMineId(null);
                        this.TileSelections[this.CurrentTileDirection] = false;
                    }
                }
                else
                {
                    tile.SetIsMineResourceVisible();
                    tile.SetMineResourceMineId(null);
                    this.TileSelections[this.CurrentTileDirection] = false;
                }
            }

            base.CompleteProcessing();
        }

        protected override void Process(double rate)
        {
            if (this.IsMiningUnknownResource && this.FactoryProgress > 0 && this.FactoryProgress < 0.99)
            {
                // Reveal new resource earlier than expected depending on density
                var tile = World.GetSmallTile(this.CurrentTileIndex);
                var actualFramesToProcess = this.GetFramesToProcess(tile, true);
                if (this.FactoryProgress > actualFramesToProcess / (double)this.framesToProcess)
                {
                    this.FactoryProgress = 0.99;
                    this.IsMiningUnknownResource = false;
                    tile.SetIsMineResourceVisible();
                }
            }

            if (!this.TileSelections[this.CurrentTileDirection])
            {
                this.producedItemType = ItemType.None;
                this.CompleteProcessing();
            }
            else
            {
                var tile = World.GetSmallTile(this.CurrentTileIndex);
                if (tile.MineResourceExtrationProgress > this.FactoryProgress) this.FactoryProgress = tile.MineResourceExtrationProgress;
                this.producedItemType = tile.MineResourceType;
                base.Process(rate);
                if (tile != null) tile.SetResourceExtractionProgress(this.FactoryProgress > 0.999 ? 0.0 : this.FactoryProgress);
            }
        }

        protected override void UpdateAnimationFrame()
        {
            if (this.animationTimer == 0)
            {
                this.animationTimer = 3;
                var frame = this.AnimationFrame;
                if (frame > 16 && this.producedItemType.In(ItemType.Coal, ItemType.Stone)) frame -= 15;

                if (this.FactoryStatus == FactoryStatus.InProgress)
                {
                    frame = frame < 16 ? frame + 1 : 2;
                    this.animationPrevIsOn = true;
                }
                else if (this.FactoryStatus == FactoryStatus.Starting)
                {
                    frame = frame > 1 ? frame : 2;
                    this.animationPrevIsOn = true;
                }
                else if (!this.animationPrevIsOn) frame = (this.FactoryStatus == FactoryStatus.Broken && World.WorldTime.Minute % 2 == 0) ? 0 : 1;
                else if (frame > 1) frame = frame < 16 ? frame + 1 : 1;
                else this.animationPrevIsOn = false;

                this.AnimationFrame = frame > 1 && this.producedItemType != ItemType.IronOre ? frame + 15 : frame;
            }
            else this.animationTimer--;
        }

        private void UpdateRemainingResources()
        {
            this.RemainingResources.Clear();

            var tiles = this.MainTile.AdjacentTiles8;
            tiles.Add(this.MainTile);
            var unexploredCount = 0;
            foreach (var tile in tiles)
            {
                var resource = tile.GetResources();
                if (resource?.IsVisible != true) unexploredCount++;
                else if (resource.Type != ItemType.None && resource.Count > 0)
                {
                    if (this.RemainingResources.ContainsKey(resource.Type)) this.RemainingResources[resource.Type] += resource.Count;
                    else this.RemainingResources.Add(resource.Type, resource.Count);
                }
            }

            if (this.IsMineExhausted == (unexploredCount > 0 || this.RemainingResources.Any()))
            {
                this.IsMineExhausted = !this.IsMineExhausted;
                EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.IsMineExhausted), this.MainTile.Row, this.ThingType);
            }
        }

        public IEnumerable<TileHighlight> GetTilesToHighlight()
        {
            for (int i = 0; i <= 8; i++)
            {
                if (!this.TileSelections[i]) continue;

                var direction = (Direction)i;
                var tile = direction == Direction.None ? this.MainTile : this.MainTile.GetTileToDirection(direction);

                var isInProgress = this.FactoryStatus == FactoryStatus.InProgress && this.CurrentTileDirection == i;
                yield return new TileHighlight(tile.Index, isInProgress, isInProgress ? 255 : 48);
            }
        }
    }
}
