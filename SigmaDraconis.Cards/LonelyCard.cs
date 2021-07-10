namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Language;
    using Interface;

    public class LonelyCard : Card
    {
        public LonelyCard() : base(CardType.Lonely, CardDisplayType.Negative)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.Happiness, -1 } };
        }

        public override string GetDescription(string colonistName)
        {
            return LanguageManager.GetCardDescription(this.Type, this.Effects[CardEffectType.Happiness]);
        }
    }
}
