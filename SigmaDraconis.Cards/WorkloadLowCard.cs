namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class WorkloadLowCard : WorkloadCard
    {
        public WorkloadLowCard() : base(CardType.WorkloadLow, CardDisplayType.Positive)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.Happiness, 2 } };
        }
    }
}
