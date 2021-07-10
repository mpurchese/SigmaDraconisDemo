namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Interface;

    public class Cold3Card : CardWithSeverity
    {
        public Cold3Card() : base(CardType.Cold3)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.WorkSpeed, -50 }, { CardEffectType.WalkSpeed, -50 }, { CardEffectType.StressRate, 3 }, { CardEffectType.HungerRate, 50 } };
        }
    }
}
