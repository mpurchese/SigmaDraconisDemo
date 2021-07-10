namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class KekDispenser : DispenserBase, IKekDispenser
    {
        public KekDispenser() : base(ThingType.KekDispenser)
        {
        }

        public KekDispenser(ISmallTile mainTile) : base(ThingType.KekDispenser, mainTile)
        {
        }

        public override void Update()
        {
            foreach (var id in this.colonistsByAccessTile.Keys.ToList())
            {
                if (World.GetThing(this.colonistsByAccessTile[id]) is IColonist c && c.ActivityType == ColonistActivityType.DrinkKek) continue;
                this.colonistsByAccessTile.Remove(id);
            }

            base.Update();
        }

        public bool TakeKek()
        {
            var success = false;
            switch (this.DispenserStatus)
            {
                case DispenserStatus.NoResource:
                case DispenserStatus.Standby:
                    var network = World.ResourceNetwork;
                    if (network != null && network.CanTakeItems(this, ItemType.Kek, 1))
                    {
                        network.TakeItems(this, ItemType.Kek, 1);
                        this.DispenserStatus = DispenserStatus.Preparing;
                        success = true;
                    }
                    break;
                case DispenserStatus.Preparing:
                    // Waiting
                    success = true;
                    break;
                case DispenserStatus.Full:
                    this.DispenserStatus = DispenserStatus.InUse;
                    WorldStats.Increment(WorldStatKeys.KekServed);
                    success = true;
                    break;
                case DispenserStatus.InUse:
                    success = true;
                    break;
            }

            this.UpdateAnimationFrame();
            return success;
        }

        private void UpdateAnimationFrame()
        {
            var frame = 1;
            switch (this.DispenserStatus)
            {
                case DispenserStatus.Standby:
                    frame = this.IsKekAvailable() ? 2 : 1;
                    break;
                case DispenserStatus.Preparing:
                case DispenserStatus.InUse:
                    frame = (2 + (int)(this.DispenserProgress * 0.32f)).Clamp(2, 32);
                    if (frame > 32) frame = 32;
                    break;
                case DispenserStatus.Full:
                    frame = 32;
                    break;
            }

            this.AnimationFrame = frame;
        }

        public override void UpdateDispenser()
        {
            switch (this.DispenserStatus)
            {
                case DispenserStatus.NoResource:
                case DispenserStatus.Standby:
                    this.DispenserStatus = World.ResourceNetwork?.CanTakeItems(this, ItemType.Kek, 1) == true ? DispenserStatus.Standby : DispenserStatus.NoResource;
                    break;
                case DispenserStatus.Preparing:
                    // Filling up
                    this.DispenserProgress += 100f / Constants.KekDispenserFramesToPrepare;
                    if (this.DispenserProgress >= 99.9f)
                    {
                        this.DispenserProgress = 100f;
                        this.DispenserStatus = DispenserStatus.Full;
                    }
                    break;
                case DispenserStatus.Full:
                    // Start emptying if switched off
                    if (!this.IsDispenserSwitchedOn) this.DispenserStatus = DispenserStatus.InUse;
                    break;
                case DispenserStatus.InUse:
                    // Slowly empty
                    this.DispenserProgress -= 100f / Constants.KekDispenserFramesToUse;
                    if (this.DispenserProgress <= 0.1f)
                    {
                        this.DispenserProgress = 0f;
                        this.DispenserStatus = DispenserStatus.Standby;
                    }
                    break;
            }

            this.UpdateAnimationFrame();
        }

        private bool IsKekAvailable()
        {
            return World.ResourceNetwork?.CanTakeItems(this, ItemType.Kek, 1) == true;
        }
    }
}
