namespace SigmaDraconis.Cards
{
    using Language;
    using Interface;

    public class WorkloadCard : Card
    {
        public WorkloadCard(CardType type, CardDisplayType displayType) : base(type, displayType)
        {
        }

        public override string GetDescription(string colonistName)
        {
            return this.Effects.ContainsKey(CardEffectType.Happiness)
                ? LanguageManager.GetCardDescription(this.Type, colonistName, this.Effects[CardEffectType.Happiness])
                : LanguageManager.GetCardDescription(this.Type, colonistName);
        }
    }
}
