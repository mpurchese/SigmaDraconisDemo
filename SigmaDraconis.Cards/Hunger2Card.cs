namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class Hunger2Card : Card
    {
        public Hunger2Card() : base(CardType.Hunger2, CardDisplayType.Warning)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.Happiness, -5 } };
        }
    }
}
