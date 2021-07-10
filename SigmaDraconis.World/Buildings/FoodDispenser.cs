namespace SigmaDraconis.World.Buildings
{
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Config;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class FoodDispenser : DispenserBase, IFoodDispenser
    {

        [ProtoMember(1, IsRequired = true)]
        public bool AllowMush { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public bool AllowFood { get; set; }

        [ProtoMember(3)]
        public int LidPosition { get; set; }

        [ProtoMember(4)]
        public ItemType CurrentFoodItemType { get; set; }

        [ProtoMember(5)]
        public int CurrentFoodType { get; set; }

        public FoodDispenser() : base(ThingType.FoodDispenser)
        {
        }

        public FoodDispenser(ISmallTile mainTile) : base(ThingType.FoodDispenser, mainTile)
        {
            this.AllowMush = true;
            this.AllowFood = true;
        }

        public override void Update()
        {
            foreach (var id in this.colonistsByAccessTile.Keys.ToList())
            {
                if (World.GetThing(this.colonistsByAccessTile[id]) is IColonist c && c.ActivityType == ColonistActivityType.Eat) continue;
                this.colonistsByAccessTile.Remove(id);
            }

            base.Update();
        }

        public bool TakeFood(int foodType, out double amount)
        {
            var success = true;
            amount = 0;

            var network = World.ResourceNetwork;
            if (network == null) return false;

            switch (this.DispenserStatus)
            {
                case DispenserStatus.Standby:
                case DispenserStatus.NoResource:
                    // Empty so begin preparing requested meal if we can
                    var itemType = foodType == Constants.MushFoodType ? ItemType.Mush : ItemType.Food;
                    if (this.IsDispenserSwitchedOn && this.IsFoodAvailable(itemType) && this.LidPosition == 0)
                    {
                        success = true;
                        if (itemType == ItemType.Mush) network.TakeItems(this, itemType, 1);
                        else network.TakeFood(this, foodType);
                        this.DispenserProgress = 0;
                        this.DispenserStatus = DispenserStatus.Preparing;
                        this.CurrentFoodType = foodType;
                        this.CurrentFoodItemType = itemType;
                    }
                    else success = false;   // Finished
                    break;
                case DispenserStatus.InUse:
                    success = true;
                    amount = Constants.ColonistEatNourishmentPerFrame;
                    break;
            }

            this.UpdateAnimationFrame();
            return success;
        }

        public override void UpdateDispenser()
        {
            switch (this.DispenserStatus)
            {
                case DispenserStatus.Standby:
                case DispenserStatus.NoResource:
                    // Close the lid if it's open
                    if (this.LidPosition > 0) this.LidPosition--;
                    this.DispenserStatus = this.IsFoodAvailable() ? DispenserStatus.Standby : DispenserStatus.NoResource;
                    break;
                case DispenserStatus.Preparing:
                    // Prepare the requested meal
                    this.DispenserProgress += 100f / Constants.FoodDispenserFramesToPrepare;
                    if (this.DispenserProgress >= 99.9f)
                    {
                        this.DispenserProgress = 100f;
                        this.DispenserStatus = DispenserStatus.Full;
                    }
                    break;
                case DispenserStatus.Full:
                    // Open the lid
                    if (this.LidPosition < 32) this.LidPosition++;
                    if (this.LidPosition >= 32)
                    {
                        this.DispenserStatus = DispenserStatus.InUse;
                        WorldStats.Increment(WorldStatKeys.MealsServed);
                    }
                    break;
                case DispenserStatus.InUse:
                    // Slowly empty - colonist can eat during this time, but if they don't then the food is lost anyway
                    this.DispenserProgress -= 100f / Constants.ColonistFramesToEat;
                    if (this.DispenserProgress <= 0.1f)
                    {
                        this.DispenserProgress = 0f;
                        this.DispenserStatus = DispenserStatus.Standby;
                        this.CurrentFoodItemType = ItemType.None;
                    }
                    break;
            }

            this.UpdateAnimationFrame();
        }

        private void UpdateAnimationFrame()
        {
            // 1       : Empty, no light (no food / blueprint)
            // 2       : Empty, green light (standby)
            // 3 - 10  : Empty, closing lid
            // 11 - 18 : Filling with mush
            // 19 - 26 : Opening lid with mush
            // 27 - 35 : Mush being eaten
            // 36 - 43 : Filling with food
            // 44 - 51 : Opening lid with food
            // 52 - 60 : Food being eaten

            var frame = 1;
            switch (this.DispenserStatus)
            {
                case DispenserStatus.Standby:
                    // Close the lid if it's open
                    if (this.LidPosition > 0)
                    {
                        frame = 3 + (this.LidPosition / 4);
                        if (frame > 10) frame = 10;
                    }
                    else
                    {
                        frame = this.IsFoodAvailable() ? 2 : 1;
                    }
                    break;
                case DispenserStatus.Preparing:
                    frame = 11 + (int)(this.DispenserProgress * 0.08f);
                    if (frame > 18) frame = 18;
                    break;
                case DispenserStatus.Full:
                    frame = 19 + (this.LidPosition / 4);
                    if (frame > 26) frame = 26;
                    break;
                case DispenserStatus.InUse:
                    frame = 35 - (int)(this.DispenserProgress * 0.09f);
                    if (frame < 27) frame = 27;
                    break;
            }

            if (frame >= 11 && frame <= 35)
            {
                var cropDefinition = CropDefinitionManager.GetDefinition(this.CurrentFoodType);
                frame += cropDefinition.AnimationStartFrame - 11;
            }

            this.AnimationFrame = frame;
        }

        public IEnumerable<int> GetFoodTypesAvailable()
        {
            var network = World.ResourceNetwork;
            if (network == null) yield break;

            if (this.AllowFood && network.CanTakeItems(this, ItemType.Food, 1))
            {
                foreach (var kv in World.GetFoodCounts()) yield return kv.Key;
            }

            if (this.AllowMush && network.CanTakeItems(this, ItemType.Mush, 1)) yield return Constants.MushFoodType;
        }

        private bool IsFoodAvailable(ItemType itemType)
        {
            return World.ResourceNetwork?.CanTakeItems(this, itemType, 1) == true;
        }

        private bool IsFoodAvailable()
        {
            var network = World.ResourceNetwork;
            if (network == null) return false;

            if (this.AllowFood && network.CanTakeItems(this, ItemType.Food, 1)) return true;
            if (this.AllowMush && network.CanTakeItems(this, ItemType.Mush, 1)) return true;

            return false;
        }
    }
}
