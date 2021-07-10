namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class VeryUnhappyCard : HappinessCard
    {
        public VeryUnhappyCard() : base(CardType.VeryUnhappy, CardDisplayType.Warning)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.OnStrike, 1 } };
        }
    }
}
