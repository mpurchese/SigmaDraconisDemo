namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class Hot2Card : Card
    {
        public Hot2Card() : base(CardType.Hot2, CardDisplayType.Negative)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.StressRate, 2 }, { CardEffectType.ThirstRate, 100 } };
        }
    }
}
