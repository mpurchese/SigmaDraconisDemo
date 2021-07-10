namespace SigmaDraconis.World.Buildings
{
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using Rooms;
    using WorldInterfaces;

    [ProtoContract]
    public class EnvironmentControl : Building, IEnvironmentControl
    {
        private const int SwitchOffEnergyLevel = 16000;
        private const int NoPowerDelay = 60;

        private int fanAnimationFrame = 1;
        private int screenAnimationFrame = 1;
        private float soundVolume;

        [ProtoMember(1)]
        public RoomLightSetting LightSetting { get; set; }

        [ProtoMember(2)]
        public RoomTemperatureSetting TemperatureSetting { get; set; }

        [ProtoMember(3)]
        public int TargetTempMin { get; set; }

        [ProtoMember(4)]
        public int NoPowerTimer { get; set; }

        [ProtoMember(5)]
        public int TargetTempMax { get; set; }

        [ProtoMember(6)]
        public bool HasBeenInCompletedRoom { get; set; }

        [ProtoMember(7)]
        public bool IsOn { get; private set; }

        public Energy EnergyUseRate { get; private set; }
        public SmoothedEnergy SmoothedEnergyUseRate { get; private set; } = new SmoothedEnergy();

        public int FanAnimationFrame
        {
            get
            {
                return this.fanAnimationFrame;
            }
            set
            {
                if (this.fanAnimationFrame != value)
                {
                    if (this.mainTile != null) EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.FanAnimationFrame), this.fanAnimationFrame, value, this.mainTile.Row, this.ThingType);
                    this.fanAnimationFrame = value;
                }
            }
        }

        public int ScreenAnimationFrame
        {
            get
            {
                return this.screenAnimationFrame;
            }
            set
            {
                if (this.screenAnimationFrame != value)
                {
                    if (this.mainTile != null) EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.ScreenAnimationFrame), this.screenAnimationFrame, value, this.mainTile.Row, this.ThingType);
                    this.screenAnimationFrame = value;
                }
            }
        }

        public EnvironmentControl() : base(ThingType.EnvironmentControl)
        {
        }

        public EnvironmentControl(ISmallTile mainTile) : base(ThingType.EnvironmentControl, mainTile, 1)
        {
        }

        public override void AfterConstructionComplete()
        {
            if (this.Room != null)
            {
                this.HasBeenInCompletedRoom = true;
                this.SwitchToDefaultSettings();
            }

            this.IsOn = true;
            base.AfterConstructionComplete();
        }

        protected override void BeforeRecycle()
        {
            if (this.IsOn) this.TogglePower();
            base.BeforeRecycle();
        }

        public void TogglePower()
        {
            this.IsOn = !this.IsOn;

            this.AnimationFrame = 1;
            this.ScreenAnimationFrame = 1;

            // Remove heat source
            if (this.mainTile.HeatSources.ContainsKey(this.Id))
            {
                this.mainTile.HeatSources.Remove(this.Id);
            }

            // Remove light if there are no other control units
            if (this.Room != null && !World.GetThings(ThingType.EnvironmentControl).OfType<IEnvironmentControl>().Any(r => r != this && r.IsReady && r.IsOn && r.Room == this.Room))
            {
                this.Room.ArtificialLight = 0;
            }
        }

        public void UpdateRoomControl(out Energy energyUsed)
        {
            energyUsed = 0;
            this.EnergyUseRate = 0;

            var network = World.ResourceNetwork;
            if (network == null) return;

            if (this.NoPowerTimer > 0) this.NoPowerTimer--;

            var prevTargetTileTemp = RoomManager.GetTileTemperature(this.mainTile.Index);

            var room = this.Room;
            if (room == null || !room.IsComplete || !this.IsReady || !this.IsOn)
            {
                this.AnimationFrame = 1;
                this.ScreenAnimationFrame = 1;
                var newTargetTemp = RoomManager.GetTileTemperature(this.mainTile.Index);
                if (prevTargetTileTemp != newTargetTemp) EventManager.IsTemperatureOverlayInvalidated = true;
                this.SmoothedEnergyUseRate.SetValue(this.EnergyUseRate);
                return;
            }

            if (!this.HasBeenInCompletedRoom)
            {
                // Room just finished construction, switch on
                this.HasBeenInCompletedRoom = true;
                this.SwitchToDefaultSettings();
            }
            
            var targetTileTemp = RoomManager.GetTileTemperature(this.mainTile.Index, this.Id);
            var roomTemp = (float)room.Temperature;
            var delta = 0f;
            if (roomTemp < this.TargetTempMin + 0.1f) delta = (this.TargetTempMin + 0.1f) - roomTemp;
            else if (roomTemp > this.TargetTempMax - 0.1f) delta = (this.TargetTempMax - 0.1f) - roomTemp;   // Will be -ve

            if ((this.LightSetting == RoomLightSetting.Off && this.TemperatureSetting == RoomTemperatureSetting.Off) || this.NoPowerTimer > 0)
            {
                this.AnimationFrame = 1;
                this.ScreenAnimationFrame = 1;
                room.ArtificialLight = 0;
                this.SmoothedEnergyUseRate.SetValue(this.EnergyUseRate);
                return;
            }

            if (!network.CanTakeEnergy(SwitchOffEnergyLevel))
            {
                this.NoPowerTimer = NoPowerDelay;
                this.AnimationFrame = 1;
                this.ScreenAnimationFrame = 1;
                room.ArtificialLight = 0;
                var newTargetTemp = RoomManager.GetTileTemperature(this.mainTile.Index);
                if (prevTargetTileTemp != newTargetTemp) EventManager.IsTemperatureOverlayInvalidated = true;
                this.SmoothedEnergyUseRate.SetValue(this.EnergyUseRate);
                return;
            }

            // Heat all tiles
            delta += (float)room.HeatLossRate;
            delta = delta * room.Roofs.Count * 5;

            delta = Mathf.Min(40, delta);
            delta = Mathf.Max(-40, delta);

            if (this.mainTile.HeatSources.ContainsKey(this.Id))
            {
                this.mainTile.HeatSources[this.Id].Amount = delta;
                this.mainTile.HeatSources[this.Id].IsOn = false;
            }
            else this.mainTile.HeatSources.Add(this.Id, new HeatOrLightSource(delta, false));

            var isRoomOccupied = (this.TemperatureSetting == RoomTemperatureSetting.Automatic || this.LightSetting == RoomLightSetting.Automatic)
                && room.Tiles.SelectMany(t => t.ThingsPrimary).Any(t => t.ThingType == ThingType.Colonist && !(t as IColonist).IsDead);

            var isFanOn = false;
            if (this.TemperatureSetting == RoomTemperatureSetting.Off || (this.TemperatureSetting == RoomTemperatureSetting.Automatic && !isRoomOccupied))
            {
                this.AnimationFrame = 1;
            }
            else
            {
                var roomSize = Room.Roofs.Count;
                if (Room.Temperature > this.TargetTempMax - 0.1f)
                {
                    energyUsed = -(int)(delta * 50);
                    this.mainTile.HeatSources[this.Id].IsOn = true;
                    room.Temperature -= 0.00001 * energyUsed.Joules / roomSize;
                    this.AnimationFrame = 3;
                    this.FanAnimationFrame = this.FanAnimationFrame == 18 ? 1 : this.FanAnimationFrame + 1;
                    isFanOn = true;
                }
                else if (Room.Temperature < this.TargetTempMin + 0.1f)
                {
                    energyUsed = (int)(delta * 50);
                    this.mainTile.HeatSources[this.Id].IsOn = true;
                    room.Temperature += 0.00002 * energyUsed.Joules / roomSize;
                    this.AnimationFrame = 2;
                }
                else
                {
                    this.AnimationFrame = 1;
                }
            }

            if (this.definitionSoundVolume > 0 && (isFanOn || this.soundVolume > 0))
            {
                this.soundVolume = (isFanOn ? 1f : 0f).Clamp(this.soundVolume - this.definitionSoundFade, this.soundVolume + this.definitionSoundFade);
                EventManager.EnqueueSoundUpdateEvent(this.id, this.soundVolume < 0.001f, this.soundVolume * this.definitionSoundVolume);
            }

            var newTargetTileTemp = RoomManager.GetTileTemperature(this.mainTile.Index);
            if (prevTargetTileTemp != newTargetTileTemp) EventManager.IsTemperatureOverlayInvalidated = true;
            
            if (isRoomOccupied && room.Tiles.SelectMany(t => t.ThingsPrimary).All(t => t.ThingType != ThingType.Colonist || (t as IColonist).Body.IsSleeping || (t as IColonist).IsResting))
            {
                isRoomOccupied = false;   // Lights off if colonists asleep or resting
            }

            // Lights
            if ((this.LightSetting == RoomLightSetting.AlwaysOn && (World.WorldLight.Brightness - 0.1f) * 1.5f < 0.6f) || (isRoomOccupied && World.WorldLight.Brightness < 0.5f && this.LightSetting == RoomLightSetting.Automatic))
            {
                var controlCount = World.GetThings(ThingType.EnvironmentControl).OfType<EnvironmentControl>().Count(r => r.IsReady && r.Room == this.Room);
                energyUsed += room.Roofs.Count * 50 / controlCount;  // 5W per tile, split between control units
                this.Room.ArtificialLight = 0.6f;
            }
            else
            {
                this.Room.ArtificialLight = 0;
            }

            // Screen animation frame depends on light and temperature settings
            var newScreenAnimationFrame = 2;
            if (this.LightSetting != RoomLightSetting.Off) newScreenAnimationFrame += this.LightSetting == RoomLightSetting.AlwaysOn ? 6 : 3;
            if (this.TemperatureSetting != RoomTemperatureSetting.Off) newScreenAnimationFrame += this.TemperatureSetting == RoomTemperatureSetting.AlwaysOn ? 2 : 1;
            this.ScreenAnimationFrame = newScreenAnimationFrame;

            // Should be enough, already checked we had at least 2000J
            network.TakeEnergy(energyUsed);
            this.EnergyUseRate = energyUsed * 3600.0;
            this.SmoothedEnergyUseRate.SetValue(this.EnergyUseRate);
        }

        public override void AfterAddedToWorld()
        {
            if (this.TargetTempMax == 0) this.TargetTempMax = 30;

            if (this.IsReady)
            {
                this.UpdateRoom();

                // Cut hole in roof
                if (this.MainTile.ThingsAll.FirstOrDefault(t => t.ThingType == ThingType.Roof) is IAnimatedThing roof) roof.AnimationFrame = 2;
            }

            base.AfterAddedToWorld();
        }

        public override void BeforeRemoveFromWorld()
        {
            // Remove hole in roof
            if (this.MainTile.ThingsAll.FirstOrDefault(t => t.ThingType == ThingType.Roof) is IAnimatedThing roof) roof.AnimationFrame = 1;

            base.BeforeRemoveFromWorld();
        }

        private void SwitchToDefaultSettings()
        {
            var otherControl = World.GetThings(ThingType.EnvironmentControl).OfType<EnvironmentControl>().Where(r => r != this && r.Room == this.Room).FirstOrDefault();
            if (otherControl != null)
            {
                // Copy settings from any existing control in room
                this.LightSetting = otherControl.LightSetting;
                this.TemperatureSetting = otherControl.TemperatureSetting;
            }
            else
            {
                // Automatic by default 
                this.LightSetting = RoomLightSetting.Automatic;
                this.TemperatureSetting = RoomTemperatureSetting.Automatic;
            }
        }

        public override string GetTextureName(int layer = 1)
        {
            return layer == 1 ? base.GetTextureName() : $"EnvironmentControlScreen_{this.ScreenAnimationFrame}";
        }
    }
}
