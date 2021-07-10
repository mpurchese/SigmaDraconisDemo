namespace SigmaDraconis.Cards
{
    using Interface;
    using System.Collections.Generic;

    public class ProgrammerCard : Card
    {
        public ProgrammerCard() : base(CardType.Programmer, CardDisplayType.Positive)
        {
            this.DisplayOrder = 2;
            this.Effects = new Dictionary<CardEffectType, int> { { CardEffectType.WorkSpeed, 20 } };
        }
    }
}
