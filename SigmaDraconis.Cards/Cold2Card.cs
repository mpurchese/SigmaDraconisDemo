namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class Cold2Card : Card
    {
        public Cold2Card() : base(CardType.Cold2, CardDisplayType.Warning)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.StressRate, 2 }, { CardEffectType.HungerRate, 50 } };
        }
    }
}
