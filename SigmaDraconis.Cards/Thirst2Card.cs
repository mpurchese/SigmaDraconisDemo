namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class Thirst2Card : Card
    {
        public Thirst2Card() : base(CardType.Thirst2, CardDisplayType.Warning)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.Happiness, -5 } };
        }
    }
}
