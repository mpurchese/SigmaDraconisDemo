namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class Hot3Card : CardWithSeverity
    {
        public Hot3Card() : base(CardType.Hot3)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.WorkSpeed, -50 }, { CardEffectType.WalkSpeed, -50 }, { CardEffectType.StressRate, 3 }, { CardEffectType.ThirstRate, 100 } };
        }
    }
}
