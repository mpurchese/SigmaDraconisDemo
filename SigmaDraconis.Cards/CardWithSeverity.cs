namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Language;
    using Interface;

    public class CardWithSeverity : Card
    {
        public int Severity { get; set; }

        public CardWithSeverity(CardType type) : base(type, CardDisplayType.Danger)
        {
            this.DisplayOrder = -1;   // Show last
        }

        public override string GetTitle()
        {
            return LanguageManager.GetCardName(this.Type, this.Severity);
        }

        public override Dictionary<string, string> GetSerializationObject()
        {
            return new Dictionary<string, string> { { "Severity", this.Severity.ToString() } };
        }

        public override void InitFromSerializationObject(Dictionary<string, string> obj)
        {
            if (obj.ContainsKey("Severity")) this.Severity = int.Parse(obj["Severity"]);
        }
    }
}
