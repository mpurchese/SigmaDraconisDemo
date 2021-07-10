namespace SigmaDraconis.World.Buildings
{
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;
    using Rooms;
    using WorldInterfaces;
    using Shared;

    [ProtoContract]
    public class DirectionalHeater : Building, IDirectionalHeater
    {
        private const int SwitchOffEnergyLevel = 20000;
        private const int NoPowerDelay = 60;

        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        // Obsolete: Use IsOn and IsAutomatic
        [ProtoMember(2)]
        public RoomTemperatureSetting HeaterSetting { get; set; }

        [ProtoMember(3)]
        public Energy EnergyUseRate { get; private set; }

        [ProtoMember(4)]
        public int TargetTemperature { get; set; }

        [ProtoMember(5)]
        public int NoPowerTimer { get; set; }

        [ProtoMember(6)]
        public bool IsIndoorMode { get; set; }

        [ProtoMember(7)]
        public bool IsOn { get; set; }

        [ProtoMember(8)]
        public bool IsAutomatic { get; set; }

        public SmoothedEnergy SmoothedEnergyUseRate { get; private set; }

        public DirectionalHeater() : base()
        {
            this.SmoothedEnergyUseRate = new SmoothedEnergy();
        }

        public DirectionalHeater(ISmallTile tile, Direction direction) : base(ThingType.DirectionalHeater, tile, 1)
        {
            this.Direction = direction;
            this.SmoothedEnergyUseRate = new SmoothedEnergy();
            switch (direction)
            {
                case Direction.SE:
                    this.animationFrame = 1; break;
                case Direction.NE:
                    this.animationFrame = 9; break;
                case Direction.NW:
                    this.animationFrame = 17; break;
                case Direction.SW:
                    this.animationFrame = 25; break;
            }
        }

        public override void AfterConstructionComplete()
        {
            this.IsOn = true;
            this.IsAutomatic = true;
            this.TargetTemperature = 16;
            base.AfterConstructionComplete();
        }

        protected override void BeforeRecycle()
        {
            var targetTile = this.mainTile.GetTileToDirection(this.Direction);
            if (targetTile != null && targetTile.HeatSources.ContainsKey(this.Id))
            {
                targetTile.HeatSources.Remove(this.Id);
                EventManager.IsTemperatureOverlayInvalidated = true;
            }

            base.BeforeRecycle();
        }

        public override void Update()
        {
            if (World.WorldTime.FrameNumber % 3 == 0)
            {
                var frame = this.AnimationFrame;
                if (frame > 32) frame -= 32;
                     
                if (this.Direction == Direction.SE && frame != 1)
                {
                    if (frame == 32) frame = 1;
                    else if (frame < 17) frame--;
                    else frame++;
                }
                else if (this.Direction == Direction.NE && frame != 9)
                {
                    if (frame == 32) frame = 1;
                    else if (frame > 9 && frame < 25) frame--;
                    else frame++;
                }
                else if (this.Direction == Direction.NW && frame != 17)
                {
                    if (frame > 17) frame--;
                    else frame++;
                }
                else if (this.Direction == Direction.SW && frame != 25)
                {
                    if (frame == 1) frame = 32;
                    else if (frame < 9 || frame > 25) frame--;
                    else frame++;
                }

                this.AnimationFrame = frame + (this.EnergyUseRate > 0 ? 32 : 0);
            }

            base.Update();
        }

        public void SetDirection(Direction direction)
        {
            if (direction == this.Direction) return;

            if (this.mainTile != null)   // May be null when called by deserialiser
            {
                var targetTile = this.mainTile.GetTileToDirection(this.Direction);
                if (targetTile != null && targetTile.HeatSources.ContainsKey(this.Id))
                {
                    targetTile.HeatSources.Remove(this.Id);
                    EventManager.IsTemperatureOverlayInvalidated = true;
                }
            }

            this.Direction = direction;
        }

        public void UpdateHeater(out Energy energyUsed)
        {
            energyUsed = 0;
            this.EnergyUseRate = 0;

            var targetTile = this.mainTile.GetTileToDirection(this.Direction);
            if (targetTile == null) return;

            if (!this.IsOn || !this.IsReady || this.mainTile.HasWallOrDoorToDirection(this.Direction))
            {
                if (targetTile.HeatSources.ContainsKey(this.Id)) targetTile.HeatSources.Remove(this.Id);
                this.SmoothedEnergyUseRate.SetValue(this.EnergyUseRate);
                return;
            }

            var prevTargetTileTemp = RoomManager.GetTileTemperature(targetTile.Index);
            var targetTileTemp = RoomManager.GetTileTemperature(targetTile.Index, this.Id);
            var delta = this.TargetTemperature - targetTileTemp;
            var room = RoomManager.GetRoom(this.MainTileIndex);
            if (this.IsIndoorMode && room != null)
            {
                // Heat all tiles
                delta += (float)room.HeatLossRate;
                delta *= room.Roofs.Count;
            }
            else if (room == null) this.IsIndoorMode = false;

            var t = Mathf.Min(room == null ? 20 : 40, delta);
            if (room != null) t = Mathf.Min(40 - targetTileTemp, t);  // Don't heat above 40C

            if (targetTile.HeatSources.ContainsKey(this.Id))
            {
                targetTile.HeatSources[this.Id].Amount = t;
                targetTile.HeatSources[this.Id].IsOn = false;
            }
            else targetTile.HeatSources.Add(this.Id, new HeatOrLightSource(t, false));

            if (this.TargetTemperature <= targetTileTemp || (this.IsAutomatic && !this.AutoSensorCheck(targetTile)))
            {
                // Standby mode
                var newTargetTemp = RoomManager.GetTileTemperature(targetTile.Index);
                if (prevTargetTileTemp != newTargetTemp) EventManager.IsTemperatureOverlayInvalidated = true;
                this.SmoothedEnergyUseRate.SetValue(this.EnergyUseRate);
                return;
            }
            else
            {
                var network = World.ResourceNetwork;
                if (network == null || !network.CanTakeEnergy(SwitchOffEnergyLevel))
                {
                    this.NoPowerTimer = NoPowerDelay;
                }
                else
                {
                    // Should be enough, already checked we had at least 2000J
                    energyUsed = (int)(t * (room == null ? 100 : 50));   // More effective indoors
                    network.TakeEnergy(energyUsed);
                    this.EnergyUseRate = energyUsed * 3600.0;
                    targetTile.HeatSources[this.Id].IsOn = true;
                    if (room != null)
                    {
                        // Heat room
                        var roomSize = Room.Roofs.Count;
                        Room.Temperature += 0.00002 * energyUsed.Joules / roomSize;
                    }
                }
            }

            var newTargetTileTemp = RoomManager.GetTileTemperature(targetTile.Index);
            if (prevTargetTileTemp != newTargetTileTemp) EventManager.IsTemperatureOverlayInvalidated = true;
            this.SmoothedEnergyUseRate.SetValue(this.EnergyUseRate);
        }

        private bool AutoSensorCheck(ISmallTile targetTile)
        {
            if (this.IsIndoorMode && RoomManager.GetRoom(this.mainTile.Index) is IRoom room)
            {
                return room.Tiles.SelectMany(t => t.ThingsPrimary).Any(t => t.ThingType == ThingType.Colonist && !(t as IColonist).IsDead);
            }

            foreach (var thing in targetTile.ThingsPrimary)
            {
                if (thing is IColonist c && !c.IsDead) return true;   // Alive colonist
                if (thing is IPlanter p && p.PlanterStatus.In(PlanterStatus.InProgress, PlanterStatus.WaitingToHarvest)) return true;  // Planter
            }

            return false;
        }
    }
}
