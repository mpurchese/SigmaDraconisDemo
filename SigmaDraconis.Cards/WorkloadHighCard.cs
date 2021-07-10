namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class WorkloadHighCard : WorkloadCard
    {
        public WorkloadHighCard() : base(CardType.WorkloadHigh, CardDisplayType.Negative)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.Happiness, -3 } };
        }
    }
}
