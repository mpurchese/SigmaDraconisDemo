namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class ColdTolerantCard : TraitCard
    {
        public ColdTolerantCard() : base(CardType.ColdTolerant)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.ColdTolerance, 5 } };
        }
    }
}
