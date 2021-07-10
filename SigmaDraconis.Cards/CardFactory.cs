namespace SigmaDraconis.Cards
{
    using System;
    using Interface;

    public static class CardFactory
    {
        public static Card Get(CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Social1:
                case CardType.Social2:
                case CardType.Social3:
                    return new SocialCard(cardType);
            }

            var type = Type.GetType($"SigmaDraconis.Cards.{Enum.GetName(typeof(CardType), cardType)}Card");
            return type != null ? Activator.CreateInstance(type) as Card : null;
        }
    }
}
