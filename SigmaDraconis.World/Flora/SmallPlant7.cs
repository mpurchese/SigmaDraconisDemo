namespace SigmaDraconis.World.Flora
{
    using System;
    using System.Collections.Generic;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    /// <summary>
    /// Sunspine
    /// </summary>
    [ProtoContract]
    public class SmallPlant7 : Plant, IAnimatedThing
    {
        [ProtoMember(1)]
        public int FlowerGrowth { get; private set; }

        public override int GrowthStage => Math.Min(5, (this.AnimationFrame + 3) / 4);
        public override int MaxGrowthStage => 5;
        public override bool HasDeadFrame => true;
        public override bool IsDead => this.AnimationFrame >= 33;
        public override bool CanFlower => true;
        public override bool IsFlowering => this.animationFrame >= 29 && this.animationFrame <= 32;

        public SmallPlant7() : base(ThingType.SmallPlant7)
        {
        }

        public SmallPlant7(ISmallTile tile) : base(ThingType.SmallPlant7, tile, 1)
        {
            var r = Rand.Next(10) + 1;
            if (r <= 4) this.AnimationFrame = (r * 4) - Rand.Next(4);        // Young,  frames 1 - 16
            else if (r <= 9) this.AnimationFrame = Rand.Next(4) + 17;        // Mature, frames 17 - 20
            else this.AnimationFrame = Rand.Next(4) + 33;                    // Dead,   frames 33 - 36
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var isDead = this.animationFrame >= 33;
            var organics = isDead ? 3 : Math.Min(((this.animationFrame - 1) / 4) + 1, 5);
            return new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, organics },
                { ItemType.IronOre, 0 }
            };
        }

        // This is called every hour.  Currently the sunspine doesn't actually grow or seed, but it does flower in the evenings.
        public override List<int> UpdateGrowth()
        {
            base.UpdateGrowth();

            if (World.WorldTime.Hour > 100 && this.AnimationFrame < 33 && this.AnimationFrame >= 17)
            {
                if (this.FlowerGrowth > 0)
                {
                    this.FlowerGrowth++;
                    if (this.FlowerGrowth == 15 || this.FlowerGrowth == 20) this.AnimationFrame += 4;
                    else if (this.FlowerGrowth == 50) this.AnimationFrame -= 12;
                }
                else if (this.FlowerGrowth == 0 && World.WorldTime.Hour <= 110 && Rand.Next(10) == 0)
                {
                    this.FlowerGrowth = 1;
                    this.AnimationFrame += 4;
                }
            }

            return null;
        }
    }
}
