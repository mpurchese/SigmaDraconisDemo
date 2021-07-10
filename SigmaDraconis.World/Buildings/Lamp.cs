namespace SigmaDraconis.World.Buildings
{
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;
    using Rooms;

    [ProtoContract]
    public class Lamp : Building, ILamp
    {
        private Energy energyPerHour;
        private Energy energyPerFrame;
        private const int SwitchOffEnergyLevel = 10000;
        private const int NoPowerDelay = 60;
        private List<ISmallTile> tilesInRange;

        // Optimisation
        private long lastSensorCheckFrame;
        private bool lastSensorCheckResult;

        [ProtoMember(1)]
        public RoomLightSetting LightSetting { get; set; }      // Obsolete: Use IsOn and IsAutomatic

        [ProtoMember(2)]
        public int NoPowerTimer { get; set; }

        [ProtoMember(3)]
        public bool IsOn { get; set; }

        [ProtoMember(4)]
        public bool IsAutomatic { get; set; }

        public Lamp() : base(ThingType.Lamp)
        {
        }

        public Lamp(ISmallTile mainTile) : base(ThingType.Lamp, mainTile, 1)
        {
            this.UpdateEnergyRequirements();
        }

        public override void AfterConstructionComplete()
        {
            this.IsOn = true;
            this.IsAutomatic = true;
            this.UpdateEnergyRequirements();
            base.AfterConstructionComplete();
        }

        public override void AfterAddedToWorld()
        {
            if (this.IsReady) this.UpdateEnergyRequirements();
            base.AfterAddedToWorld();
        }

        private void UpdateEnergyRequirements()
        {
            this.energyPerHour = Energy.FromKwH(Constants.LampEnergyUsage);
            this.energyPerFrame = this.energyPerHour / Constants.FramesPerHour;
        }

        public Energy EnergyUseRate { get; private set; }

        public void UpdateLamp(out Energy energyUsed)
        {
            energyUsed = 0; 
            this.EnergyUseRate = 0;

            var network = World.ResourceNetwork;
            if (network == null) return;

            var prevFrame = this.animationFrame;

            if (!this.IsOn || this.NoPowerTimer > 0 || !this.IsReady || (World.WorldLight.Brightness - 0.1f) * 1.5f >= 0.6f || (this.IsAutomatic && !this.AutoSensorCheck()))
            {
                this.AnimationFrame = 1;
                if (this.NoPowerTimer > 0) this.NoPowerTimer--;
            }
            else
            {
                if (!network.CanTakeEnergy(SwitchOffEnergyLevel))
                {
                    this.NoPowerTimer = NoPowerDelay;
                    this.AnimationFrame = 1;
                }
                else if (World.WorldLight.Brightness < 0.5f || !this.IsAutomatic)
                {
                    // Should be enough, already checked we had at least 2000J
                    energyUsed = this.energyPerFrame;
                    network.TakeEnergy(energyUsed);
                    this.EnergyUseRate = energyUsed * 3600.0;
                    this.AnimationFrame = 2;
                }
                else
                {
                    this.AnimationFrame = 1;
                }
            }

            if (this.animationFrame != prevFrame || this.tilesInRange == null)
            {
                this.tilesInRange = new List<ISmallTile>();

                // Expanding circle method to add or remove lights
                var openNodes = new HashSet<int> { this.MainTileIndex };
                var closedNodes = new HashSet<int> { this.MainTileIndex };
                var rowsAndThingTypesAffected = new Dictionary<int, HashSet<ThingType>>();

                var brightnessMax = 1f;
                while (openNodes.Any() && brightnessMax > 0.01f)
                {
                    brightnessMax = 0f;
                    var list = openNodes.ToList();
                    openNodes.Clear();
                    foreach (var n in list)
                    {
                        var tile = World.GetSmallTile(n);
                        this.tilesInRange.Add(tile);
                        var distance = (tile.TerrainPosition - this.mainTile.TerrainPosition).Length();
                        var brightness = distance >= 5.5f ? 0 : Mathf.Min(Mathf.Sqrt((5.5f - distance) * 0.25f), 0.6f);
                        if (brightness > brightnessMax) brightnessMax = brightness;

                        if (brightness > 0.01f)
                        {
                            foreach (var t in tile.AdjacentTiles4)
                            {
                                if (!closedNodes.Contains(t.Index))
                                {
                                    openNodes.Add(t.Index);
                                    closedNodes.Add(t.Index);
                                }
                            }
                        }

                        if (this.animationFrame == 2)
                        {
                            if (!tile.LightSources.ContainsKey(this.Id)) tile.LightSources.Add(this.Id, new HeatOrLightSource(brightness, true));
                            else
                            {
                                tile.LightSources[this.Id].Amount = brightness;
                                tile.LightSources[this.Id].IsOn = true;
                            }
                        }
                        else if (tile.LightSources.ContainsKey(this.Id)) tile.LightSources.Remove(this.Id);

                        if (!rowsAndThingTypesAffected.ContainsKey(tile.Row)) rowsAndThingTypesAffected.Add(tile.Row, new HashSet<ThingType>());
                        foreach (var type in tile.ThingsAll.Select(t => t.ThingType).Distinct())
                        {
                            if (!rowsAndThingTypesAffected[tile.Row].Contains(type)) rowsAndThingTypesAffected[tile.Row].Add(type);
                        }
                    }
                }

                foreach (var row in rowsAndThingTypesAffected)
                {
                    foreach (var type in row.Value) EventManager.EnqueueRoomLightChangeEvent(row.Key, type);
                }
            }
        }

        private bool AutoSensorCheck()
        {
            if (this.tilesInRange == null)
            {
                this.lastSensorCheckFrame = World.WorldTime.FrameNumber;
                this.lastSensorCheckResult = false;
                return false;
            }

            if (World.WorldTime.FrameNumber - 13 < this.lastSensorCheckFrame) return this.lastSensorCheckResult;  // Optimisation - only check every few frames
            this.lastSensorCheckFrame = World.WorldTime.FrameNumber;

            var allLamps = World.GetThings<ILamp>(ThingType.Lamp).Where(l => l.IsReady && !l.IsRecycling).ToList();
            foreach (var tile in this.tilesInRange)
            {
                var room = RoomManager.GetRoom(tile.Index);
                if (room?.ArtificialLight > 0) continue;    // Ignore colonists in lit room

                // Only light for colonist if this is the closest lamp, they are awake, and not waiting in a pod
                foreach (var colonist in tile.ThingsPrimary.OfType<IColonist>().Where(c => !c.Body.IsSleeping && !c.IsResting && !c.IsDead && (!c.IsWaiting || tile.ThingsPrimary.All(t => t.ThingType != ThingType.SleepPod))))
                {
                    var closestLamp = allLamps.OrderBy(l => (l.MainTile.TerrainPosition - colonist.Position).Length()).FirstOrDefault();
                    if (closestLamp == this)
                    {
                        this.lastSensorCheckResult = true;
                        return true;
                    }
                }
            }

            this.lastSensorCheckResult = false;
            return false;
        }
    }
}
