namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.UI;
    using Cards.Interface;
    using Language;
    using Shared;
    using World;
    using WorldInterfaces;

    public class ColonistPortraitTooltip : Tooltip
    {
        private readonly TextLabel activityLabel;
        private readonly List<TextLabel> warningLabels = new List<TextLabel>();
        private readonly VerticalStack labelStack;

        private readonly int colonistId;

        public ColonistPortraitTooltip(IUIElement parent, IUIElement attachedElement, IColonist colonist)
            : base(parent, attachedElement, Scale(160), Scale(156), $"{colonist.ShortName} - {LanguageManager.Get<SkillType>(colonist.Skill)}".ToUpperInvariant())
        {
            this.colonistId = colonist.Id;
            
            this.activityLabel = new TextLabel(this, 0, Scale(18), this.W, Scale(14), "", UIColour.DefaultText, TextAlignment.TopCentre) { AnchorRight = true };
            this.AddChild(this.activityLabel);

            // All other labels are children of this.labelStack
            this.labelStack = new VerticalStack(this, 0, Scale(38), this.W, Scale(156), true) { AnchorRight = true };
            this.AddChild(this.labelStack);

            this.labelStack.LayoutInvalidated = true;
        }

        protected override void HandleLanguageChange()
        {
            if (World.GetThing(this.colonistId) is IColonist colonist) this.SetTitle($"{colonist.ShortName} - {LanguageManager.Get<SkillType>(colonist.Skill)}".ToUpperInvariant());
            base.HandleLanguageChange();
        }

        public override void Update()
        {
            if (!this.attachedElement.IsMouseOver || !this.IsEnabled) return;

            base.Update();

            var layoutInvalidated = false;
            if (!(World.GetThing(this.colonistId) is IColonist colonist)) return;

            if (colonist.IsDead)
            {
                if (colonist.Body.DeathReason == DeathReason.None) this.activityLabel.Text = LanguageManager.Get<StringsForColonistPanel>(StringsForColonistPanel.ColonistIsDead, colonist.ShortName);
                else this.activityLabel.Text = LanguageManager.Get<StringsForColonistPanel>(StringsForColonistPanel.ColonistDiedOf, colonist.ShortName, Enum.GetName(typeof(DeathReason), colonist.Body.DeathReason).ToLowerInvariant());

                foreach (var child in this.Children.Where(c => c != this.titleLabel && c != this.activityLabel && c.IsVisible))
                {
                    child.IsVisible = false;
                    layoutInvalidated = true;
                }
            }
            else
            {
                this.activityLabel.Text = colonist.CurrentActivityDescription;

                // Merge social cards
                var distinctCards = new List<ICard>();
                var socialCount = 0;
                foreach (var card in colonist.Cards.Cards.Values.OrderByDescending(c => c.DisplayOrder))
                {
                    if (card.Type == CardType.Social1 || card.Type == CardType.Social2 || card.Type == CardType.Social3)
                    {
                        if (socialCount == 0)
                        {
                            distinctCards.Add(card);
                            socialCount = 1;
                        }
                        else socialCount++;
                    }
                    else distinctCards.Add(card);
                }

                var row = 0;
                foreach (var card in distinctCards)
                {
                    var colour = UIColour.DefaultText;
                    switch (card.DisplayType)
                    {
                        case CardDisplayType.Trait: colour = UIColour.LightBlueText; break;
                        case CardDisplayType.Positive: colour = UIColour.GreenText; break;
                        case CardDisplayType.Negative: colour = UIColour.YellowText; break;
                        case CardDisplayType.Warning: colour = UIColour.OrangeText; break;
                        case CardDisplayType.Danger: colour = UIColour.RedText; break;
                    }

                    var title = card.GetTitle();
                    if (socialCount > 1 && (card.Type == CardType.Social1 || card.Type == CardType.Social2 || card.Type == CardType.Social3))
                    {
                        title = $"{title} x{socialCount}";
                    }

                    if (row >= this.warningLabels.Count)
                    {

                        var label = new TextLabel(this.labelStack, 0, 0, this.W, Scale(14), title, colour, TextAlignment.TopCentre) { AnchorRight = true };
                        this.labelStack.AddChild(label);
                        this.warningLabels.Add(label);
                        layoutInvalidated = true;
                    }
                    else
                    {
                        if (this.warningLabels[row].Text == "") layoutInvalidated = true;
                        this.warningLabels[row].Text = title;
                        this.warningLabels[row].Colour = colour;
                    }

                    row++;
                }

                for (int i = row; i < this.warningLabels.Count; i++)
                {
                    if (this.warningLabels[i].Text != "")
                    {
                        this.warningLabels[i].Text = "";
                        layoutInvalidated = true;
                    }
                }
            }

            if (this.appliedScale != UIStatics.Scale) layoutInvalidated = true;

            this.UpdateWidthAndHeight();
            this.ApplyLayout();
            if (layoutInvalidated) this.labelStack.LayoutInvalidated = true;
        }

        public override void ApplyLayout()
        {
            this.titleLabel.W = this.W;
            this.titleLabel.H = Scale(14);
            this.activityLabel.W = this.W;
            this.activityLabel.H = Scale(14);

            foreach (var label in this.warningLabels)
            {
                label.W = this.W;
                label.H = Scale(14);
            }

            if (this.appliedScale != UIStatics.Scale)
            {
                this.activityLabel.Y = Scale(18);
                this.labelStack.Y = Scale(38);
                this.labelStack.LayoutInvalidated = true;
                this.appliedScale = UIStatics.Scale;
            }

            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        protected override void UpdateWidthAndHeight()
        {
            this.W = (Math.Max(this.titleLabel.Text.Length, this.Children.OfType<TextLabel>().Union(this.warningLabels).Max(l => l.Text.Length)) + 4) * UIStatics.TextRenderer.LetterSpace;
            this.H = (this.warningLabels.Count(l => l.Text != "") * Scale(16)) + Scale(40);
        }
    }
}
