namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class NeutralHappinessCard : HappinessCard
    {
        public NeutralHappinessCard() : base(CardType.NeutralHappiness, CardDisplayType.Neutral)
        {
            this.Effects = new Dictionary<CardEffectType, int>();
        }
    }
}
