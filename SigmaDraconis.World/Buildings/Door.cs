namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Language;
    using Rooms;
    using WorldInterfaces;
    using Shared;

    [ProtoContract]
    public class Door : Building, IDoor
    {
        protected float soundVolume;

        private int animationDelay = 0;
        public bool IsOpen => this.AnimationFrame > 1;

        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        [ProtoMember(2)]
        private int closeTimer;

        [ProtoMember(3)]
        public DoorState State { get; private set; }

        public override TileBlockModel TileBlockModel => this.State == DoorState.LockedClosed ? TileBlockModel.Wall : TileBlockModel.Door;

        public Door() : base(ThingType.Door)
        {
            this.canRecycleReasonStringId = StringsForMouseCursor.SupportingRoof;
        }

        public Door(ISmallTile mainTile, Direction direction) : base(ThingType.Door, mainTile, new List<ISmallTile> { mainTile, mainTile.GetTileToDirection(direction) })
        {
            this.canRecycleReasonStringId = StringsForMouseCursor.SupportingRoof;
            this.Direction = direction;
        }

        // Special constructor for when we are replacing a wall with a door, to fix problem that path finding was not updating correctly
        public Door(ISmallTile mainTile, int constructionProgress, Direction direction) : base(ThingType.Door)
        {
            this.id = ++nextId;
            this.ThingType = ThingType.Door;
            this.ConstructionProgress = constructionProgress;
            this.constructionProgressExact = constructionProgress;
            this.SetPosition(mainTile, new List <ISmallTile> { mainTile, mainTile.GetTileToDirection(direction) });
            this.RenderAlpha = 1;
            this.ShadowAlpha = 1;
            this.Direction = direction;
        }

        public void SetState(DoorState newState)
        {
            if (newState == this.State) return;

            var raiseEvent = (this.State == DoorState.LockedClosed) != (newState == DoorState.LockedClosed);
            this.State = newState;
            if (raiseEvent) EventManager.RaiseEvent(EventType.Door, EventSubType.Updated, this);  // For pathfinding

            if (this.animationFrame == 1 && newState == DoorState.LockedClosed) this.AnimationFrame = 0;
            else if (this.animationFrame == 0 && newState != DoorState.LockedClosed) this.AnimationFrame = 1;
            else if (this.animationFrame == 8 && newState == DoorState.LockedOpen) this.AnimationFrame = 9;
            else if (this.animationFrame == 9 && newState != DoorState.LockedOpen) this.AnimationFrame = 8;
        }

        public void Open()
        {
            this.closeTimer = 30;
        }

        public override void Update()
        {
            base.Update();

            if (!this.IsReady)
            {
                if (this.AnimationFrame > 1) this.AnimateClose();
                return;
            }

            var prevFrame = this.AnimationFrame;

            if ((this.closeTimer > 0 || this.State == DoorState.LockedOpen) && this.AnimationFrame < 8) this.AnimateOpen();
            else if (this.closeTimer == 0 && this.AnimationFrame > 1 && this.State != DoorState.LockedOpen) this.AnimateClose();

            if (this.closeTimer > 0) this.closeTimer--;

            this.UpdateSound();

            if (this.AnimationFrame != prevFrame) this.UpdateShadowModel();
        }

        public void UpdateSound()
        {
            var volume = 0f;
            var pitch = 0f;
            if (this.AnimationFrame > 1 && this.AnimationFrame < 8)
            {
                volume = 1f;
                pitch = (this.AnimationFrame - 2) / 20f;
            }

            if (this.definitionSoundVolume > 0 && (volume > 0 || this.soundVolume > 0))
            {
                this.soundVolume = volume.Clamp(this.soundVolume - this.definitionSoundFade, this.soundVolume + this.definitionSoundFade);
                EventManager.EnqueueSoundUpdateEvent(this.id, this.soundVolume < 0.001f, this.soundVolume * this.definitionSoundVolume, pitch: pitch);
            }
        }

        private void AnimateClose()
        {
            if (this.animationDelay == 0)
            {
                var next = this.animationFrame - 1;
                if (next == 8) next--;
                if (next == 1 && this.State == DoorState.LockedClosed) next = 0;
                this.AnimationFrame = next;
                this.animationDelay = 1;
            }
            else
            {
                this.animationDelay--;
            }
        }

        private void AnimateOpen()
        {
            if (this.animationDelay == 0)
            {
                var next = this.animationFrame + 1;
                if (next == 1) next++;
                if (next == 8 && this.State == DoorState.LockedOpen) next = 9;

                this.AnimationFrame = next;
                this.animationDelay = 1;
            }
            else
            {
                this.animationDelay--;
            }
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{base.GetTextureName()}_{this.Direction.ToString()}";
        }

        public override bool CanRecycle()
        {
            // Can't deconstruct external wall, i.e. one with a roof in only one direction.  So we do an XOR here.
            if (this.mainTile.ThingsPrimary.Any(t => t.ThingType == ThingType.Roof) ^ this.mainTile.GetTileToDirection(this.Direction).ThingsPrimary.Any(t => t.ThingType == ThingType.Roof)) return false;
            return base.CanRecycle();
        }

        public override void AfterConstructionComplete()
        {
            base.AfterConstructionComplete();
            RoomManager.SplitRoom(this);
        }
    }
}
