namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Cards.Interface;
    using WorldInterfaces;
    
    public class ColonistPanelCardStrip : UIElementBase
    {
        private readonly Dictionary<CardType, ColonistPanelCard> displayedCards = new Dictionary<CardType, ColonistPanelCard>();
        private readonly Dictionary<CardType, CardTooltip> displayedTooltips = new Dictionary<CardType, CardTooltip>();

        private IColonist colonist;

        public ColonistPanelCardStrip(IUIElement parent, int x, int y)
            : base(parent, x, y, Scale(266), Scale(30))
        {
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>("Textures\\Cards\\CardStripBackground");
            base.LoadContent();
        }

        public void SetColonist(IColonist newColonist)
        {
            if (newColonist != this.colonist)
            {
                this.colonist = newColonist;

                //var cardsToRemove = this.displayedCards.Keys.Where(c => !this.colonist.Cards.Contains(c)).ToList();
                foreach (var c in this.displayedCards.Values) this.RemoveChild(c);
                foreach (var c in this.displayedTooltips.Values) TooltipParent.Instance.RemoveChild(c);

                this.displayedCards.Clear();
                this.displayedTooltips.Clear();

                this.UpdateCards();
            }
        }

        public override void Update()
        {
            if (this.colonist.IsDead)
            {
                foreach (var c in this.displayedCards.Keys.ToList())
                {
                    this.RemoveChild(this.displayedCards[c]);
                    this.displayedCards.Remove(c);

                    TooltipParent.Instance.RemoveChild(this.displayedTooltips[c]);
                    this.displayedTooltips.Remove(c);
                }
            }
            else if (this.colonist.Cards.IsDisplayInvalidated) this.UpdateCards();

            base.Update();
        }

        private void UpdateCards()
        {
            // Remove
            var cardsToRemove = this.displayedCards.Keys.Where(c => !this.colonist.Cards.Contains(c)).ToList();
            foreach (var c in cardsToRemove)
            {
                this.RemoveChild(this.displayedCards[c]);
                this.displayedCards.Remove(c);

                TooltipParent.Instance.RemoveChild(this.displayedTooltips[c]);
                this.displayedTooltips.Remove(c);
            }

            // Add
            var cardsToAdd = this.colonist.Cards.Cards.Where(c => c.Value.IsVisible && !this.displayedCards.ContainsKey(c.Key));
            foreach (var kv in cardsToAdd)
            {
                if (!this.displayedCards.ContainsKey(kv.Key)) displayedCards.Add(kv.Key, new ColonistPanelCard(this, 0, 1, kv.Value));
                var card = this.displayedCards[kv.Key];
                this.AddChild(card);

                if (!this.displayedTooltips.ContainsKey(kv.Key)) this.displayedTooltips.Add(kv.Key, new CardTooltip(this, card));
                var tooltip = this.displayedTooltips[kv.Key];
                tooltip.SetColonist(this.colonist);
                TooltipParent.Instance.AddChild(tooltip);
            }

            // Update positions and text.  Squash cards together if we need to.
            if (this.displayedCards.Any())
            {
                var spacePerCard = Math.Min(((this.W - 2) / this.displayedCards.Count * 2) / 2, Scale(26));
                var pinnedX = 0;
                var unpinnedX = (this.W / 2) - (this.displayedCards.Count(c => c.Value.DisplayOrder <= 1) * spacePerCard / 2);
                foreach (var card in this.displayedCards.OrderByDescending(c => c.Value.DisplayOrder))
                {
                    if (card.Value.DisplayOrder > 1)
                    {
                        card.Value.X = pinnedX;
                        pinnedX += spacePerCard;
                        if (unpinnedX < pinnedX) unpinnedX = pinnedX;
                    }
                    else
                    {
                        card.Value.X = unpinnedX;
                        unpinnedX += spacePerCard;
                    }

                    this.displayedTooltips[card.Key].InvalidateText();
                }
            }

            this.colonist.Cards.IsDisplayInvalidated = false;
        }
    }
}
