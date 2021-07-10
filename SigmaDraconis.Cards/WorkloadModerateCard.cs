namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class WorkloadModerateCard : WorkloadCard
    {
        public WorkloadModerateCard() : base(CardType.WorkloadModerate, CardDisplayType.Neutral)
        {
            this.Effects = new Dictionary<CardEffectType, int>();
        }
    }
}
