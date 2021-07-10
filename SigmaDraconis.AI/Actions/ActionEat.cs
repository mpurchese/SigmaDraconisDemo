namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using Config;
    using Shared;
    using World;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionEat : ActionBase
    {
        [ProtoMember(1)]
        private double totalNourishment;

        [ProtoMember(2)]
        private readonly int foodType;

        [ProtoMember(3)]
        private readonly int dispenserID;

        // Deserialisation ctor
        protected ActionEat() { }

        public ActionEat(IColonist colonist, int dispenserID, int foodType) : base(colonist)
        {
            this.dispenserID = dispenserID;
            this.foodType = foodType;
        }

        public override void Update()
        {
            if (!(World.GetThing(this.dispenserID) is IFoodDispenser dispenser))
            {
                this.IsFinished = true;
                return;
            }

            this.OpenDoorIfExists();
            this.Colonist.IsWorking = false;

            if (!dispenser.TakeFood(this.foodType, out double nourishment))
            {
                // Dispenser is empty or something went wrong
                this.IsFinished = true;
                if (this.totalNourishment > 0) this.AddCommentary();
                return;
            }

            if (nourishment > 0)
            {
                this.Colonist.Eat(this.foodType, nourishment, totalNourishment == 0.0);
                totalNourishment += nourishment;

                if (this.Colonist.Body.Nourishment >= 99.9)
                {
                    this.IsFinished = true;
                    this.AddCommentary();
                    return;
                }
            }

            base.Update();
        }

        private void AddCommentary()
        {
            var foodName = CropDefinitionManager.GetDefinition(this.foodType)?.DisplayNameLower;
            if (foodName == null) return;

            this.Colonist.UpdateDietCard(out int varietyEffect, out int lastMealRepeatCount);

            var opinion = this.Colonist.GetFoodOpinion(this.foodType);
            if (opinion.HasValue)
            {
                if (varietyEffect < 0 && lastMealRepeatCount > 2 && opinion > 0)
                {
                    // Nice but more variety please!
                    EventManager.EnqueueColonistEvent(ColonistEventType.FoodLikeButLacksVariety, this.Colonist.Id);
                }
                else if (varietyEffect > 0 && lastMealRepeatCount < 2 && opinion > 0)
                {
                    EventManager.EnqueueColonistEvent(ColonistEventType.FoodGoodVariety, this.Colonist.Id);
                }
                else if (varietyEffect < 0 && lastMealRepeatCount > 1 && opinion == 0)
                {
                    // More variety please
                    EventManager.EnqueueColonistEvent(ColonistEventType.FoodLacksVariety, this.Colonist.Id);
                }
                else if (varietyEffect < 0 && lastMealRepeatCount > 1 && opinion < 0)
                {
                    // Repeat dislike
                    EventManager.EnqueueColonistEvent(ColonistEventType.FoodDislikeLacksVariety, this.Colonist.Id);
                }
                else if (opinion > 0)
                {
                    // Like
                    EventManager.EnqueueColonistEvent(ColonistEventType.AteLike, this.Colonist.Id);
                }
                else if (opinion < 0)
                {
                    // Dislike
                    EventManager.EnqueueColonistEvent(ColonistEventType.AteDislike, this.Colonist.Id);
                }
            }
            else
            {
                // New food types
                opinion = this.Colonist.GetFoodOpinion(this.foodType, true);
                if (opinion == 0)
                {
                    EventManager.EnqueueColonistEvent(ColonistEventType.AteNewNeutral, this.Colonist.Id);
                }
                else if (opinion > 0)
                {
                    EventManager.EnqueueColonistEvent(ColonistEventType.AteNewLike, this.Colonist.Id);
                }
                else
                {
                    EventManager.EnqueueColonistEvent(ColonistEventType.AteNewDislike, this.Colonist.Id);
                }
            }
        }
    }
}
