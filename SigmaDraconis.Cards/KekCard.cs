namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Language;
    using Interface;

    public class KekCard : Card
    {
        public int RemainingHours { get; set; }

        public KekCard() : base(CardType.Kek, CardDisplayType.Positive)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.Happiness, 2 } };
        }

        public override string GetDescription(string colonistName)
        {
            return LanguageManager.GetCardDescription(this.Type, this.RemainingHours);
        }

        public override Dictionary<string, string> GetSerializationObject()
        {
            return new Dictionary<string, string> { { "RemainingHours", this.RemainingHours.ToString() } };
        }

        public override void InitFromSerializationObject(Dictionary<string, string> obj)
        {
            if (obj.ContainsKey("RemainingHours")) this.RemainingHours = int.Parse(obj["RemainingHours"]);
        }
    }
}
