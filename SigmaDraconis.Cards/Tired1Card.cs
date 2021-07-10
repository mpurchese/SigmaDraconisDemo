namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class Tired1Card : Card
    {
        public Tired1Card() : base(CardType.Tired1, CardDisplayType.Neutral)
        {
            this.Effects = new Dictionary<CardEffectType, int>();
        }
    }
}
