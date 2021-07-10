namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using WorldInterfaces;
    using Shared;

    [ProtoContract]
    public class Battery : Building, IBattery
    {
        private Energy chargeLevel;

        public Battery() : base(ThingType.Battery)
        {
        }

        public Battery(ISmallTile smallTile) : base(ThingType.Battery, smallTile, 1)
        {
        }

        public Energy ChargeCapacity { get { return Energy.FromKwH(Constants.BatteryEnergyStorage); } }

        public override void AfterDeserialization()
        {
            // Capacity reduced in v0.2
            if (this.chargeLevel > this.ChargeCapacity) this.chargeLevel = this.ChargeCapacity;

            base.AfterDeserialization();
        }

        [ProtoMember(1)]
        public Energy ChargeLevel
        {
            get
            {
                return this.chargeLevel;
            }
            set
            {
                if (this.chargeLevel != value)
                {
                    this.chargeLevel = value;
                    if (value < this.ChargeCapacity * 0.05) this.AnimationFrame = 1;
                    else if (value < this.ChargeCapacity * 0.33) this.AnimationFrame = 2;
                    else if (value < this.ChargeCapacity * 0.66) this.AnimationFrame = 3;
                    else if (value < this.ChargeCapacity * 0.95) this.AnimationFrame = 4;
                    else this.AnimationFrame = 5;
                }
            }
        }
    }
}
