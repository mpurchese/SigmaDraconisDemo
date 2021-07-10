namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Language;
    using Interface;

    public class HappinessCard : Card
    {
        public HappinessCard(CardType type, CardDisplayType displayType) : base(type, displayType)
        {
            this.DisplayOrder = 2;
        }

        public int Happiness { get; set; }

        public override string GetDescription(string colonistName)
        {
            var sign = this.Happiness >= 0 ? "+" : ""; 
            return LanguageManager.GetCardDescription(this.Type, colonistName, $"{sign}{this.Happiness}");
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
