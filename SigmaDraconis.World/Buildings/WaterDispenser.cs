namespace SigmaDraconis.World.Buildings
{
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class WaterDispenser : DispenserBase, IWaterDispenser
    {
        public WaterDispenser() : base(ThingType.WaterDispenser)
        {
        }

        public WaterDispenser(ISmallTile mainTile) : base(ThingType.WaterDispenser, mainTile)
        {
        }

        public override void Update()
        {
            foreach (var id in this.colonistsByAccessTile.Keys.ToList())
            {
                if (World.GetThing(this.colonistsByAccessTile[id]) is IColonist c && c.ActivityType == ColonistActivityType.Drink) continue;
                this.colonistsByAccessTile.Remove(id);
            }

            base.Update();
        }

        public bool TakeWater(out double amount)
        {
            var success = false;
            amount = 0;
            switch (this.DispenserStatus)
            {
                case DispenserStatus.NoResource:
                case DispenserStatus.Standby:
                    if (World.ResourceNetwork?.CanTakeItems(this, ItemType.Water, 100) == true)
                    {
                        World.ResourceNetwork.TakeItems(this, ItemType.Water, 100);
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
                    amount = Constants.ColonistDrinkHydrationPerFrame;
                    WorldStats.Increment(WorldStatKeys.WaterServed);
                    success = true;
                    break;
                case DispenserStatus.InUse:
                    amount = Constants.ColonistDrinkHydrationPerFrame;
                    success = true;
                    break;
            }

            this.UpdateAnimationFrame();
            return success;
        }

        private void UpdateAnimationFrame()
        {
            // Animation frames:
            // 1       : Empty
            // 2 - 16  : Filling
            // 17      : Full
            // 18 - 32 : Emptying
            // 33      : Empty with light on
            // 34 - 48 : Filling with light on
            // 49      : Full with light on
            // 50 - 64 : Emptying with light on

            var frame = 1;
            switch (this.DispenserStatus)
            {
                case DispenserStatus.Preparing:
                    frame = 2 + (int)(this.DispenserProgress * 0.16f);
                    if (frame > 16) frame = 16;
                    break;
                case DispenserStatus.Full:
                    frame = 17;
                    break;
                case DispenserStatus.InUse:
                    frame = 32 - (int)(this.DispenserProgress * 0.16f);
                    if (frame < 18) frame = 18;
                    break;
            }

            // Light comes on in the dark
            if (frame > 0 && Rooms.RoomManager.GetTileLightLevel(this.MainTileIndex) < 0.2f) frame += 32;

            this.AnimationFrame = frame;
        }

        public override void UpdateDispenser()
        {
            switch (this.DispenserStatus)
            {
                case DispenserStatus.NoResource:
                case DispenserStatus.Standby:
                    this.DispenserStatus = World.ResourceNetwork?.CanTakeItems(this, ItemType.Water, 100) == true ? DispenserStatus.Standby : DispenserStatus.NoResource;
                    break;
                case DispenserStatus.Preparing:
                    // Filling up
                    this.DispenserProgress += 100f / Constants.WaterDispenserFramesToPrepare;
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
                    // Slowly empty - colonist can drink during this time, but if they don't then the water is lost anyway
                    this.DispenserProgress -= 100f / Constants.ColonistFramesToDrink;
                    if (this.DispenserProgress <= 0.1f)
                    {
                        this.DispenserProgress = 0f;
                        this.DispenserStatus = DispenserStatus.Standby;
                    }
                    break;
            }

            this.UpdateAnimationFrame();
        }
    }
}
