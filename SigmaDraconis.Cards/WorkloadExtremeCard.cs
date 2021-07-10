namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class WorkloadExtremeCard : WorkloadCard
    {
        public WorkloadExtremeCard() : base(CardType.WorkloadExtreme, CardDisplayType.Warning)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.Happiness, -6 } };
        }
    }
}
