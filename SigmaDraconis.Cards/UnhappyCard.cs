namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class UnhappyCard : HappinessCard
    {
        public UnhappyCard() : base(CardType.Unhappy, CardDisplayType.Warning)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.WorkSpeed, -30 } };
        }
    }
}
