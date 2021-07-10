namespace SigmaDraconis.Cards.Interface
{
    using System.Collections.Generic;

    public interface ICard
    {
        CardType Type { get; }
        CardDisplayType DisplayType { get; }
        bool IsVisible { get; set; }
        int DisplayOrder { get; }
        Dictionary<CardEffectType, int> Effects { get; }

        string GetDescription(string colonistName);
        string GetTitle();
        string GetTexturePath();
        Dictionary<string, string> GetSerializationObject();
        void InitFromSerializationObject(Dictionary<string, string> obj);
    }
}
