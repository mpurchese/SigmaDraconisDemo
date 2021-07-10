namespace SigmaDraconis.World.Buildings
{
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using Particles;
    using Zones;
    using WorldInterfaces;
    using Draconis.Shared;

    [ProtoContract]
    public class LandingPod : Building, ILandingPod
    {
        private string loadedSound = "";
        private float soundVolume;

        [ProtoMember(1)]
        private bool isTileBlockApplied;

        [ProtoMember(2)]
        public bool IsEmpty { get; private set; }

        [ProtoMember(3)]
        public int AnimationDelayTimer { get; set; }

        [ProtoMember(4)]
        protected Dictionary<int, int> colonistsByAccessTile;

        [ProtoMember(5)]
        public float Altitude { get; set; }

        [ProtoMember(6)]
        public float VerticalSpeed { get; set; }

        public bool RequiresAccessNow => true;

        public override int RecycleTime => 300;

        public LandingPod() : base(ThingType.LandingPod)
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        public LandingPod(ISmallTile mainTile) : base(ThingType.LandingPod, mainTile, 1)
        {
            this.AnimationDelayTimer = 120;
            this.VerticalSpeed = -1f;
            this.RenderAlpha = 0f;
            this.ShadowAlpha = 0f;
            this.ConstructionProgress = 100;
            this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        public override Vector2f GetWorldPosition()
        {
            var tilePos = base.GetWorldPosition();
            return new Vector2f(tilePos.X, tilePos.Y - this.Altitude);
        }

        public override void BeforeRemoveFromWorld()
        {
            EventManager.EnqueueSoundRemoveEvent(this.id);
            base.BeforeRemoveFromWorld();
        }

        public override void Update()
        {
            if (!this.isTileBlockApplied)
            {
                PathFinderBlockManager.AddBlock(this.Id, this.mainTile, Direction.N, TileBlockType.All);
                PathFinderBlockManager.AddBlock(this.Id, this.mainTile, Direction.E, TileBlockType.All);
                PathFinderBlockManager.AddBlock(this.Id, this.mainTile, Direction.S, TileBlockType.All);
                PathFinderBlockManager.AddBlock(this.Id, this.mainTile, Direction.W, TileBlockType.All);
                PathFinderBlockManager.AddBlock(this.Id, this.mainTile, Direction.NW, TileBlockType.All);
                PathFinderBlockManager.AddBlock(this.Id, this.mainTile, Direction.NE, TileBlockType.All);
                PathFinderBlockManager.AddBlock(this.Id, this.mainTile, Direction.SW, TileBlockType.All);
                PathFinderBlockManager.AddBlock(this.Id, this.mainTile, Direction.SE, TileBlockType.All);
                ZoneManager.HomeZone.UpdateNode(this.MainTileIndex);
                ZoneManager.GlobalZone.UpdateNode(this.MainTileIndex);
                ZoneManager.AnimalZone.UpdateNode(this.MainTileIndex);
                EventManager.RaiseEvent(EventType.Zone, null);
                this.isTileBlockApplied = true;
            }
            else if (!this.IsEmpty && this.mainTile.ThingsPrimary.All(t => t.ThingType != ThingType.Colonist))
            {
                PathFinderBlockManager.AddBlock(this.Id, this.mainTile, Direction.S, TileBlockType.All);
                ZoneManager.HomeZone.UpdateNode(this.MainTileIndex);
                ZoneManager.GlobalZone.UpdateNode(this.MainTileIndex);
                ZoneManager.AnimalZone.UpdateNode(this.MainTileIndex);
                EventManager.RaiseEvent(EventType.Zone, null);
                this.IsEmpty = true;
            }

            var addedSmoke = false;
            var prevAltitude = this.Altitude;
            if (this.Altitude > 0)
            {
                this.RenderAlpha = (0.01f * (200f - this.Altitude)).Clamp(0f, 1f);
                this.ShadowAlpha = this.RenderAlpha;

                if (this.Altitude > 2f && this.Altitude < 100f)
                {
                    // Produce fewer particles with higher alpha if framerate is low
                    var alphaScale = 1f;
                    var fps = PerfMonitor.UpdateFramesPerSecond;
                    if (fps < 56)
                    {
                        alphaScale = fps < 32 ? 4 : 2;
                    }

                    var smokeCount = 8 / alphaScale;
                    for (int i = 0; i < smokeCount; i++)
                    {
                        LanderExhaustSimulator.AddParticle(this.mainTile, 4f, 1f, Altitude + 0.8f, -3f, alphaScale);
                        LanderExhaustSimulator.AddParticle(this.mainTile, -4f, 1f, Altitude + 0.8f, -3f, alphaScale);
                        LanderExhaustSimulator.AddParticle(this.mainTile, 0f, -1.4f, Altitude + 0.8f, -3f, alphaScale);
                        addedSmoke = true;
                    }
                }

                this.Altitude += this.VerticalSpeed;
                this.VerticalSpeed = 0 - ((this.Altitude * 0.01f).Clamp(0.1f, 1.0f));
                if (this.Altitude < 0f) this.Altitude = 0f;

                EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.AnimationFrame), this.mainTile.Row, this.ThingType);
                EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.Altitude), this.mainTile.Row, this.ThingType);
            }
            else if (this.AnimationDelayTimer > 0) this.AnimationDelayTimer--;
            else if (this.AnimationFrame < 36) this.AnimationFrame++;

            if (addedSmoke && loadedSound != "LandingPod")
            {
                EventManager.EnqueueSoundAddEvent(this.id, "LandingPod");
                loadedSound = "LandingPod";
            }
            else if (this.Altitude == 0f && prevAltitude > 0f)
            {
                if (loadedSound != "LandingPod2")
                {
                    EventManager.EnqueueSoundRemoveEvent(this.id);
                    EventManager.EnqueueSoundAddEvent(this.id, "LandingPod2", false);
                    loadedSound = "LandingPod2";
                }

                EventManager.EnqueueSoundUpdateEvent(this.id, false, 1f);
            }

            if (this.Altitude > 0f) EventManager.EnqueueSoundUpdateEvent(this.id, !addedSmoke, addedSmoke ? 1f : 0f, this.Altitude);
            else if (this.AnimationFrame == 36)
            {
                if (loadedSound != "")
                {
                    EventManager.EnqueueSoundRemoveEvent(this.id);
                    loadedSound = "";
                }
            }
            else if (this.AnimationFrame > 1)
            {
                if (loadedSound != "Door")
                {
                    EventManager.EnqueueSoundRemoveEvent(this.id);
                    EventManager.EnqueueSoundAddEvent(this.id, "Door");
                    loadedSound = "Door";
                }

                var pitch = (this.AnimationFrame - 2) / 120f;
                this.soundVolume = 1f.Clamp(this.soundVolume - 0.1f, this.soundVolume + 0.1f);
                EventManager.EnqueueSoundUpdateEvent(this.id, this.soundVolume < 0.001f, this.soundVolume * 0.3f, pitch: pitch);
            }

            base.Update();
        }


        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var result = new Dictionary<ItemType, int>
            {
                { ItemType.Biomass, 0 },
                { ItemType.Metal, 12 },
                { ItemType.IronOre, 0 }
            };

            return result;
        }

        public IEnumerable<ISmallTile> GetAllAccessTiles()
        {
            for (int i = 0; i <= 7; i++)   // NE, SE, SW, NW
            {
                var direction = (Direction)i;
                var tile = this.mainTile.GetTileToDirection(direction);
                if (tile == null || !tile.CanWorkInTile || this.MainTile.HasWallToDirection(direction)) continue;   // Can't work here
                yield return tile;
            }
        }

        public IEnumerable<ISmallTile> GetAccessTiles(int? colonistId = null)
        {
            this.CleanupColonistAssignments();
            if (colonistId.HasValue && this.colonistsByAccessTile.Any(c => c.Value != colonistId)) yield break;
            else if (!colonistId.HasValue && this.colonistsByAccessTile.Any()) yield break;

            var done = new HashSet<int>();
            for (int i = 0; i <= 7; i++)
            {
                var direction = (Direction)i;
                var t = this.mainTile.GetTileToDirection(direction);
                if (t == null || done.Contains(t.Index) || this.allTiles.Contains(t)) continue;
                if (this.mainTile.ThingsPrimary.Any(a => a is IColonist c && (colonistId == null || c.Id != colonistId) && !c.IsMoving && !c.IsRelaxing)) continue;   // Blocked by another colonist
                if (!t.CanWorkInTile || this.mainTile.HasWallToDirection(direction)) continue;   // Can't work here
                done.Add(t.Index);
                yield return t;
            }
        }

        public bool CanAssignColonist(int colonistId, int? tileIndex = null)
        {
            this.CleanupColonistAssignments();
            if (this.colonistsByAccessTile.ContainsValue(colonistId)) return true;
            if (this.colonistsByAccessTile.Any()) return false;

            return tileIndex.HasValue
                ? this.GetAccessTiles(colonistId).Any(t => t.Index == tileIndex.Value)
                : this.GetAccessTiles(colonistId).Any();
        }

        public void AssignColonist(int colonistId, int tileIndex)
        {
            if (!this.colonistsByAccessTile.ContainsKey(tileIndex)) this.colonistsByAccessTile.Add(tileIndex, colonistId);
            else this.colonistsByAccessTile[tileIndex] = colonistId;
        }

        private void CleanupColonistAssignments()
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
            foreach (var id in this.colonistsByAccessTile.Keys.ToList())
            {
                if (World.GetThing(this.colonistsByAccessTile[id]) is IColonist c && c.ActivityType == ColonistActivityType.Deconstruct && !c.IsDead) continue;
                this.colonistsByAccessTile.Remove(id);
            }
        }
    }
}
