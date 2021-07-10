namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Language;
    using Interface;
    using Shared;

    public class RoamCard : Card
    {
        public int RemainingHours { get; set; }

        public RoamCard() : base(CardType.Roam, CardDisplayType.Positive)
        {
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.Happiness, 2 } };
            this.RemainingHours = Constants.ColonistRoamCardTimeout;
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
