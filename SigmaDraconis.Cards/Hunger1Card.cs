namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class Hunger1Card : Card
    {
        public Hunger1Card() : base(CardType.Hunger1, CardDisplayType.Neutral)
        {
            this.Effects = new Dictionary<CardEffectType, int>();
        }
    }
}
