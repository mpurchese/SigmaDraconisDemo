namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Language;
    using Interface;

    public class SleepGoodCard : Card
    {
        public SleepGoodCard() : base(CardType.SleepGood, CardDisplayType.Positive)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.Happiness, 1 } };
        }

        public override string GetDescription(string colonistName)
        {
            return LanguageManager.GetCardDescription(this.Type, colonistName, this.Effects[CardEffectType.Happiness]);
        }
    }
}
