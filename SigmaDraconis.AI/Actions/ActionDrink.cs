namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using Shared;
    using World;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionDrink : ActionBase
    {
        [ProtoMember(2)]
        private readonly int dispenserID;

        // Deserialisation ctor
        protected ActionDrink() { }

        public ActionDrink(IColonist colonist, int dispenserID) : base(colonist)
        {
            this.dispenserID = dispenserID;
        }

        public override void Update()
        {
            if (!(World.GetThing(this.dispenserID) is IWaterDispenser dispenser))
            {
                this.IsFinished = true;
                return;
            }

            this.OpenDoorIfExists();
            this.Colonist.IsWorking = false;
            
            if ((this.Colonist.Body.Hydration > 60 && dispenser.DispenserStatus == DispenserStatus.Standby) || !dispenser.TakeWater(out double amount))
            {
                // Dispenser is empty or something went wrong
                this.IsFinished = true;
                return;
            }

            if (amount > 0)
            {
                this.Colonist.Drink(amount);
                if (this.Colonist.Body.Hydration >= 99.9 || (this.Colonist.Body.Hydration > 60 && dispenser.DispenserStatus == DispenserStatus.Standby))
                {
                    this.IsFinished = true;
                    return;
                }
            }

            base.Update();
        }
    }
}
