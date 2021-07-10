namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class Tired2Card : Card
    {
        public Tired2Card() : base(CardType.Tired2, CardDisplayType.Warning)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.Happiness, -3 } };
        }
    }
}
