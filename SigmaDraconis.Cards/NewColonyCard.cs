namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Language;
    using Interface;

    public class NewColonyCard : Card
    {
        public int RemainingHours { get; set; }

        public NewColonyCard() : base(CardType.NewColony, CardDisplayType.Positive)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.Happiness, 2 } };
            this.DisplayOrder = 1;
        }

        public override string GetDescription(string colonistName)
        {
            return LanguageManager.GetCardDescription(this.Type, colonistName, this.RemainingHours);
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
