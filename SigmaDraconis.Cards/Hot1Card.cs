namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class Hot1Card : Card
    {
        public Hot1Card() : base(CardType.Hot1, CardDisplayType.Neutral)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.StressRate, 1 }, { CardEffectType.ThirstRate, 50 } };
        }
    }
}
