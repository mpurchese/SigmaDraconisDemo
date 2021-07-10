namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class WorkOutsideCard : Card
    {
        public WorkOutsideCard() : base(CardType.WorkOutside, CardDisplayType.Negative)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.WorkSpeed, -20 } };
        }
    }
}
