namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class Cold1Card : Card
    {
        public Cold1Card() : base(CardType.Cold1, CardDisplayType.Neutral)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.StressRate, 1 }, { CardEffectType.HungerRate, 20 } };
        }
    }
}
