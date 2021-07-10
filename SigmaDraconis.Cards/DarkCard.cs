namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class DarkCard : Card
    {
        public DarkCard() : base(CardType.Dark, CardDisplayType.Negative)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.WorkSpeed, -40 }, { CardEffectType.WalkSpeed, -40 } };
        }
    }
}
