namespace SigmaDraconis.World.Buildings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Projects;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class OreScanner : Building, IOreScanner
    {
        private int animationTimer;
        private Energy energyPerFrame;

        public static int MaxRange => ProjectManager.GetDefinition(205)?.IsDone == true ? Constants.OreScannerRangeImproved : Constants.OreScannerRangeBasic;

        [ProtoMember(1)]
        public FactoryStatus FactoryStatus { get; private set; }

        [ProtoMember(2)]
        public bool IsSwitchedOn { get; private set; }

        [ProtoMember(3)]
        public int CurrentRadius { get; private set; }

        [ProtoMember(4)]
        public int FramesScanned { get; private set; }

        [ProtoMember(5)]
        public double Progress { get; private set; }

        [ProtoMember(6)]
        public Energy EnergyUseRate { get; private set; }

        [ProtoMember(7)]
        public bool IsAutoRestartEnabled { get; private set; }

        [ProtoMember(8)]
        public int CurrentTileCount { get; private set; }

        [ProtoMember(9)]
        public int TimeRemainingFrames { get; private set; }

        private OreScanner() : base(ThingType.OreScanner)
        {
            this.energyPerFrame = Energy.FromKwH(Constants.OreScannerEnergyUse / 3600.0);
        }

        public OreScanner(ISmallTile mainTile) : base(ThingType.OreScanner, mainTile, 1)
        {
            this.energyPerFrame = Energy.FromKwH(Constants.OreScannerEnergyUse / 3600.0);
        }

        public override void AfterConstructionComplete()
        {
            this.IsSwitchedOn = true;
            this.CurrentRadius = 1;
            this.IsAutoRestartEnabled = true;
            base.AfterConstructionComplete();
        }

        public override void Update()
        {
            if (this.IsDesignatedForRecycling) this.IsSwitchedOn = false;
            base.Update();
        }

        public void UpdateScanner(out Energy energyUsed)
        {
            energyUsed = new Energy();

            if (!this.IsSwitchedOn && this.AnimationFrame == 1)
            {
                // Switched off and stopped.
                base.Update();
                return;
            }

            var network = World.ResourceNetwork;
            if (network == null) return;

            this.animationTimer++;
            if (this.animationTimer >= 3)
            {
                this.animationTimer = 0;

                // Recalculate tile counts every few frames (expensive)
                this.CurrentTileCount = 0;
                while (this.IsSwitchedOn && this.CurrentRadius <= MaxRange && this.CurrentTileCount == 0)
                {
                    this.CurrentTileCount = this.GetTilesToScan().Count();
                    if (this.CurrentTileCount == 0)
                    {
                        this.CurrentRadius++;
                        this.FramesScanned = 0;
                        this.Progress = 0;
                    }
                }
            }

            // Finished scanning?
            if (this.IsSwitchedOn && this.CurrentTileCount == 0)
            {
                if (this.animationTimer == 0)
                {
                    if (this.AnimationFrame >= 38) this.AnimationFrame = 9;
                    else if (this.AnimationFrame > 9) this.AnimationFrame++;
                    else if (this.AnimationFrame > 2) this.AnimationFrame--;
                    else if (this.AnimationFrame == 1) this.AnimationFrame = 2;
                }

                this.FactoryStatus =FactoryStatus.ScanComplete;
                this.Progress = 1.0;
                this.EnergyUseRate = 0;
                base.Update();
                return;
            }

            var requiresEnergy = this.IsSwitchedOn && this.AnimationFrame >= 9 && this.CurrentTileCount > 0;
            var hasEnergy = false;
            if (requiresEnergy && network.CanTakeEnergy(this.energyPerFrame))
            {
                hasEnergy = true;
                this.EnergyUseRate = this.energyPerFrame * 3600;
                network.TakeEnergy(this.energyPerFrame);
                energyUsed = this.energyPerFrame;
            }
            else this.EnergyUseRate = 0;

            // Switched off or no power?
            if (!this.IsSwitchedOn || (requiresEnergy && !hasEnergy))
            {
                if (this.animationTimer == 0)
                {
                    if (this.AnimationFrame >= 38) this.AnimationFrame = 9;
                    else if (this.AnimationFrame > 9) this.AnimationFrame++;
                    else if (this.AnimationFrame > 3) this.AnimationFrame--;
                    else this.AnimationFrame = 1;
                }

                this.FactoryStatus = this.IsSwitchedOn ? FactoryStatus.NoPower : FactoryStatus.Offline;
                base.Update();
                return;
            }

            if (this.animationTimer == 0)
            {
                if (this.AnimationFrame == 38) this.AnimationFrame = 9;
                else if (this.AnimationFrame > 1) this.AnimationFrame++;
                else if (this.AnimationFrame == 1) this.AnimationFrame = 3;
            }

            var requiredFrames = this.CurrentTileCount * (ProjectManager.GetDefinition(204)?.IsDone == true ? Constants.OreScannerFramesPerTileImproved : Constants.OreScannerFramesPerTileImproved);
            if (this.AnimationFrame >= 9) this.FramesScanned++;
            this.Progress = this.FramesScanned / (double)requiredFrames;
            this.TimeRemainingFrames = requiredFrames - this.FramesScanned;
            if (this.TimeRemainingFrames <= 0)
            {
                this.TimeRemainingFrames = 0;
                ISmallTile tileForComment = null;
                foreach (var tile in this.GetTilesToScan())
                {
                    tile.SetIsMineResourceVisible();
                    if (tile.MineResourceDensity >= MineResourceDensity.Medium && (tileForComment == null || tile.MineResourceDensity > tileForComment.MineResourceDensity))
                    {
                        tileForComment = tile;
                    }
                }

                if (tileForComment != null)
                {
                    // Prod a geologist to tell us about the new resource
                    var colonist = World.GetThings<IColonist>(ThingType.Colonist).Where(c => !c.IsDead && !c.Body.IsSleeping).OrderBy(c => Guid.NewGuid()).FirstOrDefault();
                    if (colonist != null) colonist.OreScannerTileForComment = tileForComment.Index;
                }

                if (this.CurrentRadius >= MaxRange) this.FactoryStatus = FactoryStatus.ScanComplete;
                else
                {
                    this.CurrentRadius++;
                    this.Progress = 0;
                    this.FramesScanned = 0;
                    if (this.IsAutoRestartEnabled) this.FactoryStatus = FactoryStatus.InProgress;
                    else
                    {
                        this.IsSwitchedOn = false;
                        this.FactoryStatus = FactoryStatus.Offline;
                    }
                }
            }
            else this.FactoryStatus = FactoryStatus.InProgress;

            base.Update();
        }

        public void TogglePower()
        {
            this.IsSwitchedOn = !this.IsSwitchedOn;
            if (this.IsSwitchedOn
                && (this.FactoryStatus == FactoryStatus.Offline || this.FactoryStatus == FactoryStatus.Paused 
                || (this.FactoryStatus == FactoryStatus.ScanComplete && this.CurrentRadius < MaxRange)))
            {
                this.FactoryStatus = FactoryStatus.InProgress;
            }
        }

        public void ToggleAutoRestart()
        {
            this.IsAutoRestartEnabled = !this.IsAutoRestartEnabled;
        }

        private IEnumerable<ISmallTile> GetTilesToScan()
        {
            for (int x = this.mainTile.X - this.CurrentRadius; x <= this.mainTile.X + this.CurrentRadius; x++)
            {
                if (x < 0 || x >= World.Width * 3) continue;
                for (int y = this.mainTile.Y - this.CurrentRadius; y <= this.mainTile.Y + this.CurrentRadius; y++)
                {
                    if (y < 0 || y >= World.Width * 3) continue;
                    var tile = World.GetSmallTile(x, y);
                    if (tile == null || tile.TerrainType != TerrainType.Dirt || tile.IsMineResourceVisible) continue;

                    var rx = 10 - Math.Abs(x - this.mainTile.X);
                    var ry = 10 - Math.Abs(y - this.mainTile.Y);
                    var r = Constants.OreScanRadiusMap[rx, ry];
                    if (r == this.CurrentRadius)
                    {
                        tile.OreScannerLstFrame = World.WorldTime.FrameNumber;
                        yield return tile;
                    }
                }
            }
        }

        public IEnumerable<TileHighlight> GetTilesToHighlight()
        {
            var maxRange = ProjectManager.GetDefinition(205)?.IsDone == true ? Constants.OreScannerRangeImproved : Constants.OreScannerRangeBasic;
            for (int x = this.mainTile.X - maxRange; x <= this.mainTile.X + maxRange; x++)
            {
                if (x < 0 || x >= World.Width * 3) continue;
                for (int y = this.mainTile.Y - maxRange; y <= this.mainTile.Y + maxRange; y++)
                {
                    if (y < 0 || y >= World.Width * 3) continue;
                    var tile = World.GetSmallTile(x, y);
                    if (tile == null || tile.TerrainType != TerrainType.Dirt || tile.IsMineResourceVisible) continue;

                    var isInProgress = false;
                    var rx = 10 - Math.Abs(x - this.mainTile.X);
                    var ry = 10 - Math.Abs(y - this.mainTile.Y);
                    var r = Constants.OreScanRadiusMap[rx, ry];

                    if (r > maxRange) continue;
                    if (r == this.CurrentRadius && this.FactoryStatus == FactoryStatus.InProgress) isInProgress = true;
                    
                    yield return new TileHighlight(tile.Index, isInProgress, isInProgress ? 255 : 48);
                }
            }
        }
    }
}
