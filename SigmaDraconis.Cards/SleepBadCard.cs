namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Language;
    using Interface;

    public class SleepBadCard : Card
    {
        public SleepBadCard() : base(CardType.SleepBad, CardDisplayType.Negative)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.Happiness, -1 } };
        }

        public override string GetDescription(string colonistName)
        {
            return LanguageManager.GetCardDescription(this.Type, colonistName, this.Effects[CardEffectType.Happiness]);
        }
    }
}
