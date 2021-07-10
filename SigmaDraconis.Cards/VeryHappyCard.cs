namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class VeryHappyCard : HappinessCard
    {
        public VeryHappyCard() : base(CardType.VeryHappy, CardDisplayType.Positive)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.WorkSpeed, 20 } };
        }
    }
}
