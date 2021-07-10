namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Language;
    using Interface;

    public abstract class Card : ICard
    {
        public CardDisplayType DisplayType { get; protected set; }
        public CardType Type { get; protected set; }
        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; protected set; }

        public Dictionary<CardEffectType, int> Effects { get; protected set; }

        public Card(CardType type, CardDisplayType displayType)
        {
            this.Type = type;
            this.DisplayType = displayType;
        }

        public virtual string GetTexturePath()
        {
            var name = this.GetType().Name;
            if (name.EndsWith("Card")) name = name.Substring(0, name.Length - 4);  // Remove the "Card" at the end
            return "Textures\\Cards\\" + name;
        }

        public virtual string GetTitle()
        {
            return LanguageManager.GetCardName(this.Type);
        }

        public virtual string GetDescription(string colonistName)
        {
            return LanguageManager.GetCardDescription(this.Type, colonistName);
        }

        public virtual Dictionary<string, string> GetSerializationObject()
        {
            return null;
        }

        public virtual void InitFromSerializationObject(Dictionary<string, string> obj)
        {
        }
    }
}
