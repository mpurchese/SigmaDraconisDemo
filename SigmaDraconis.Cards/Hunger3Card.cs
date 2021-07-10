namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class Hunger3Card : CardWithSeverity
    {
        public Hunger3Card() : base(CardType.Hunger3)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.WorkSpeed, -50 }, { CardEffectType.WalkSpeed, -50 } };
        }
    }
}
