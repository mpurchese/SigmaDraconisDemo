namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Language;
    using Interface;

    public class NewArrivalCard : Card
    {
        public int Happiness
        {
            get { return this.Effects[CardEffectType.Happiness]; }
            set { this.Effects[CardEffectType.Happiness] = value; }
        }

        public NewArrivalCard() : base(CardType.NewArrival, CardDisplayType.Positive)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.Happiness, 6 } };
            this.DisplayOrder = 1;
        }

        public override string GetDescription(string colonistName)
        {
            return LanguageManager.GetCardDescription(this.Type, colonistName, this.Happiness);
        }

        public override Dictionary<string, string> GetSerializationObject()
        {
            return new Dictionary<string, string> { { "Happiness", this.Happiness.ToString() } };
        }

        public override void InitFromSerializationObject(Dictionary<string, string> obj)
        {
            if (obj.ContainsKey("Happiness")) this.Happiness = int.Parse(obj["Happiness"]);
        }
    }
}
