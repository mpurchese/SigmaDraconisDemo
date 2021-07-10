namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class FastWalkCard : TraitCard
    {
        public FastWalkCard() : base(CardType.FastWalk)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.WalkSpeed, 20 } };
        }
    }
}
