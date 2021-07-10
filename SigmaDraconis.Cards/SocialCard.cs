namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Language;
    using Interface;

    public class SocialCard : Card
    {
        public string OtherColonistName { get; private set; }

        public SocialCard(CardType type, string otherColonistName = "") : base(type, CardDisplayType.Positive)
        {
            this.OtherColonistName = otherColonistName;
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.Happiness, 1 } };
        }

        public override string GetDescription(string colonistName)
        {
            return LanguageManager.GetCardDescription(this.Type, this.OtherColonistName);
        }

        public override Dictionary<string, string> GetSerializationObject()
        {
            return new Dictionary<string, string> { { "OtherColonistName", this.OtherColonistName } };
        }

        public override void InitFromSerializationObject(Dictionary<string, string> obj)
        {
            if (obj.ContainsKey("OtherColonistName")) this.OtherColonistName = obj["OtherColonistName"];
        }

        public override string GetTexturePath()
        {
            return "Textures\\Cards\\Social";
        }
    }
}
