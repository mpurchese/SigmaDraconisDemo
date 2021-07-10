namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class HeatTolerantCard : TraitCard
    {
        public HeatTolerantCard() : base(CardType.HeatTolerant)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.HeatTolerance, 5 } };
        }
    }
}
