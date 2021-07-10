namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using Rooms;
    using WorldInterfaces;

    [ProtoContract]
    public class SleepPod : Building, ISleepPod, IRotatableThing
    {
        private const int SwitchOffEnergyLevel = 1800;
        protected float soundVolume;

        [ProtoMember(1)]
        private Dictionary<int, int> colonistsByAccessTile;

        [ProtoMember(2)]
        private int doorCloseDelay;

        [ProtoMember(3)]
        public Direction Direction { get; private set; }

        [ProtoMember(4)]
        public int? OwnerID { get; set; }

        [ProtoMember(5)]
        public int OwnerChangeTimer { get; set; }

        [ProtoMember(6)]
        public float Temperature { get; set; }

        [ProtoMember(7)]
        public bool IsHeaterSwitchedOn { get; set; }

        [ProtoMember(8)]
        public Energy EnergyUseRate { get; private set; }

        [ProtoMember(9)]
        public int TargetTemp { get; set; }

        public bool RequiresAccessNow => this.IsReady;

        private int animationTimer;

        public SleepPod() : base(ThingType.SleepPod)
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
            if (this.TargetTemp == 0) this.TargetTemp = 20;
        }

        public SleepPod(ISmallTile mainTile, Direction direction) : base(ThingType.SleepPod, mainTile, 1)
        {
            this.Direction = direction;
            this.colonistsByAccessTile = new Dictionary<int, int>();
            this.Temperature = RoomManager.GetTileTemperature(this.MainTileIndex);
            this.TargetTemp = 20;
        }

        public override void AfterConstructionComplete()
        {
            this.IsHeaterSwitchedOn = true;
            this.Temperature = RoomManager.GetTileTemperature(this.MainTileIndex);
            base.AfterConstructionComplete();
        }

        public override void Recycle()
        {
            foreach (var colonist in this.MainTile.ThingsPrimary.OfType<IColonist>())
            {
                // For revealing dead colonists when recycle pod
                EventManager.MovedColonists.AddIfNew(colonist.Id);
            }

            base.Recycle();
        }

        public override void Update()
        {
            if (!this.IsReady)
            {
                if (this.AnimationFrame % 11 > 1) this.AnimationFrame--;
                else this.AnimationFrame = (this.AnimationFrame % 11) + 1;
                base.Update();
                return;
            }

            if (this.OwnerChangeTimer > 0) this.OwnerChangeTimer--;

            // Tend to environment temperature
            var environmentTemperature = RoomManager.GetTileTemperature(this.MainTileIndex);
            this.Temperature += (environmentTemperature - this.Temperature) * 0.002f;

            // Powered heating / cooling
            var isOn = this.EnergyUseRate > 0;
            this.EnergyUseRate = 0;
            if (this.IsHeaterSwitchedOn && this.MainTile.ThingsPrimary.OfType<IColonist>().Any())
            {
                if (this.Temperature < this.TargetTemp && (isOn || this.Temperature < this.TargetTemp - 0.5f))
                {
                    if (World.ResourceNetwork?.CanTakeEnergy(SwitchOffEnergyLevel) == true)
                    {
                        var e = (int)((this.TargetTemp - this.Temperature) * 2000f);
                        if (e > 100) e = 100;
                        this.EnergyUseRate = Energy.FromKwH(e * 0.002);
                        World.ResourceNetwork.TakeEnergy(this.EnergyUseRate / 3600f);
                        this.Temperature += e * 0.0005f;
                    }
                }
            }

            // Frames:
            // 0 = locked
            // 1 = unlocked
            // 9 = open
            var newFrame = this.AnimationFrame % 11;
            var open = false;
            var isLocked = false;
            foreach (var colonist in this.mainTile.ThingsAll.OfType<IColonist>())
            {
                // Colonist must be moving into our out of our tile, and not in the corner as this means they are just walking past.
                if ((colonist.Position.X % 1).Between(0.4f, 0.6f) && (colonist.Position.Y % 1).Between(0.4f, 0.6f)) continue;
                if (colonist.MainTileIndex == this.MainTileIndex && (colonist.Body.IsSleeping || colonist.IsResting || colonist.IsWaiting))
                {
                    isLocked = true;
                    continue;
                }

                open = true;

                if (open && newFrame < 10) colonist.WaitForDoor = true;
            }

            if (isLocked)
            {
                // Fix for case where colonist gets stuck in a pod tile where another is sleeping
                foreach (var colonist in this.mainTile.ThingsAll.OfType<IColonist>()) colonist.WaitForDoor = false;
            }

            if (!isLocked && open) this.doorCloseDelay = 60;
            else if (this.doorCloseDelay > 0) this.doorCloseDelay--;

            if (this.animationTimer > 0) this.animationTimer--;

            if (isLocked && this.doorCloseDelay == 0)
            {
                // Inside - close and lock the door
                if (newFrame > 0 && this.animationTimer == 0)
                {
                    newFrame--;
                    this.animationTimer = 2;
                }
            }
            else if (open)
            {
                if (newFrame < 10 && this.animationTimer == 0)
                {
                    newFrame++;
                    this.animationTimer = 2;
                }
            }
            else if (this.doorCloseDelay == 0) // No colonist
            {
                if (newFrame > 1 && this.animationTimer == 0)
                {
                    newFrame--;
                    this.animationTimer = 2;
                }
            }

            if (this.OwnerID.GetValueOrDefault() > 0) newFrame += 11;
            this.AnimationFrame = newFrame;

            foreach (var id in this.colonistsByAccessTile.Keys.ToList())
            {
                if (World.GetThing(this.colonistsByAccessTile[id]) is IColonist c && c.ActivityType == ColonistActivityType.Sleep) continue;
                this.colonistsByAccessTile.Remove(id);
            }

            this.UpdateSound();

            base.Update();
        }

        public void UpdateSound()
        {
            var volume = 0f;
            var pitch = 0f;
            var frame = this.animationFrame % 11;
            if (frame > 1 && frame < 10)
            {
                volume = 1f;
                pitch = (frame - 4) / 30f;
            }

            if (this.definitionSoundVolume > 0 && (volume > 0 || this.soundVolume > 0))
            {
                this.soundVolume = volume.Clamp(this.soundVolume - this.definitionSoundFade, this.soundVolume + this.definitionSoundFade);
                EventManager.EnqueueSoundUpdateEvent(this.id, this.soundVolume < 0.001f, this.soundVolume * this.definitionSoundVolume, pitch: pitch);
            }
        }

        public IEnumerable<ISmallTile> GetAllAccessTiles()
        {
            yield return this.mainTile.GetTileToDirection(this.Direction);
        }

        public IEnumerable<ISmallTile> GetAccessTiles(int? colonistId = null)
        {
            if (!this.IsReady) return new List<ISmallTile>(0);
            if (colonistId.HasValue && this.OwnerID.HasValue && colonistId != this.OwnerID) return new List<ISmallTile>(0);   // Someone else owns the pod
            if (this.mainTile.ThingsPrimary.OfType<IColonist>().Any(c => !colonistId.HasValue || c.Id != colonistId.Value)) return new List<ISmallTile>(0);  // Someone in the pod

            if (colonistId.HasValue && this.colonistsByAccessTile.ContainsKey(this.mainTile.Index) && this.colonistsByAccessTile[this.mainTile.Index] != colonistId)
            {
                if (World.GetThing(this.colonistsByAccessTile[this.mainTile.Index]) is IColonist colonist)
                {
                    if (colonist.ActivityType.In(ColonistActivityType.Sleep, ColonistActivityType.Rest, ColonistActivityType.GetWarm) && ((!colonist.Body.IsSleeping && !colonist.IsResting && !colonist.IsWaiting) || colonist.MainTileIndex == this.MainTileIndex))
                    {
                        return new List<ISmallTile>(0);  // Assigned to someone else
                    }
                    else
                    {
                        this.colonistsByAccessTile.Clear();
                    }
                }
            }

            return new List<ISmallTile>(1) { this.mainTile };
        }

        public bool CanAssignColonist(int colonistId, int? tileIndex = null)
        {
            return this.GetAccessTiles(colonistId).Any();
        }

        public void AssignColonist(int colonistId, int tileIndex)
        {
            if (!this.colonistsByAccessTile.ContainsKey(tileIndex)) this.colonistsByAccessTile.Add(tileIndex, colonistId);
            else this.colonistsByAccessTile[tileIndex] = colonistId;
        }

        public int? GetAssignedColonistId()
        {
            return this.colonistsByAccessTile?.ContainsKey(this.MainTileIndex) == true ? this.colonistsByAccessTile[this.MainTileIndex] : (int?)null;
        }

        public override bool CanRecycle()
        {
            if (this.MainTile.ThingsAll.Any(t => t.ThingType == ThingType.Colonist && !(t as IColonist).IsDead)) return false;  // Pod is in use
            return base.CanRecycle();
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{base.GetTextureName()}_{this.Direction.ToString()}";
        }
    }
}
