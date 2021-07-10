namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Language;
    using Interface;

    public class BadMoodCard : Card
    {
        public BadMoodCard() : base(CardType.BadMood, CardDisplayType.Negative)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.Happiness, -1 } };
        }

        public override string GetDescription(string colonistName)
        {
            return LanguageManager.GetCardDescription(this.Type, colonistName, this.Effects[CardEffectType.Happiness]);
        }

        public override Dictionary<string, string> GetSerializationObject()
        {
            return new Dictionary<string, string> { { "Happiness", this.Effects[CardEffectType.Happiness].ToString() } };
        }

        public override void InitFromSerializationObject(Dictionary<string, string> obj)
        {
            if (obj?.ContainsKey("Happiness") == true) this.Effects[CardEffectType.Happiness] = int.Parse(obj["Happiness"]);
        }
    }
}
