namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class Thirst3Card : CardWithSeverity
    {
        public Thirst3Card() : base(CardType.Thirst3)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.WorkSpeed, -30 }, { CardEffectType.WalkSpeed, -30 } };
        }
    }
}
