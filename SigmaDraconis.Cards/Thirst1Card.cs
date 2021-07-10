namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class Thirst1Card : Card
    {
        public Thirst1Card() : base(CardType.Thirst1, CardDisplayType.Neutral)
        {
            this.Effects = new Dictionary<CardEffectType, int>();
        }
    }
}
