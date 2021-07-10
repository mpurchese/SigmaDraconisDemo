namespace SigmaDraconis.Cards
{
    using Interface;

    public class TraitCard : Card
    {
        public TraitCard(CardType type) : base(type, CardDisplayType.Trait)
        {
            this.DisplayOrder = 3;
        }
    }
}
