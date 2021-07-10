namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class HappyCard : HappinessCard
    {
        public HappyCard() : base(CardType.Happy, CardDisplayType.Positive)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.WorkSpeed, 10 }, { CardEffectType.WorkWalkSpeed, 10 } };
        }
    }
}
