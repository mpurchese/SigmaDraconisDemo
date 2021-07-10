namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class WorkaholicCard : TraitCard
    {
        public WorkaholicCard() : base(CardType.Workaholic)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.Workaholism, 1 } };
        }
    }
}
