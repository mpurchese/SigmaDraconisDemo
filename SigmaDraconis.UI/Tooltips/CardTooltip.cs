namespace SigmaDraconis.UI
{
    using System.Collections.Generic;
    using Draconis.UI;
    using Cards.Interface;
    using WorldInterfaces;

    public class CardTooltip : Tooltip
    {
        private readonly List<TextLabel> labels = new List<TextLabel>();
        private string colonistName = "";
        private bool isInvalidated;

        public CardTooltip(IUIElement parent, IUIElement attachedElement)
            : base(parent, attachedElement, Scale(360), Scale(44), "")
        {
            if (this.attachedElement is ColonistPanelCard card)
            {
                switch (card.DisplayType)
                {
                    case CardDisplayType.Trait: this.SetTitle(card.GetTitle(), UIColour.LightBlueText); break;
                    case CardDisplayType.Positive: this.SetTitle(card.GetTitle(), UIColour.GreenText); break;
                    case CardDisplayType.Negative: this.SetTitle(card.GetTitle(), UIColour.YellowText); break;
                    case CardDisplayType.Warning: this.SetTitle(card.GetTitle(), UIColour.YellowText); break;
                    case CardDisplayType.Danger: this.SetTitle(card.GetTitle(), UIColour.RedText); break;
                    default: this.SetTitle(card.GetTitle()); break;
                }
            }
        }

        public void SetColonist(IColonist colonist)
        {
            this.colonistName = colonist.ShortName;
            this.isInvalidated = true;
        }

        public void InvalidateText()
        {
            this.isInvalidated = true;
        }

        public override void ApplyLayout()
        {
            var y = Scale(18);
            foreach (var label in this.labels)
            {
                label.W = this.W;
                label.H = Scale(14);
                label.Y = y;
                y += UIStatics.TextRenderer.LineHeight;
            }

            base.ApplyLayout();
        }

        public override void Update()
        {
            if (this.isInvalidated && this.attachedElement is ColonistPanelCard card)
            {
                this.labels.Clear();
                this.RemoveChildrenExceptTitle();
                this.SetTitle(card.GetTitle());

                var description = card.GetDescription(this.colonistName);
                var lines = description.Split('|');
                var texts = new List<string>();
                foreach (var line in lines) texts.Add(line);

                var y = Scale(18);
                foreach (var text in texts)
                {
                    var label = new TextLabel(this, 0, y, this.W, Scale(14), text, UIColour.DefaultText);
                    this.AddChild(label);
                    this.labels.Add(label);
                    y += UIStatics.TextRenderer.LineHeight;
                }

                this.isInvalidated = false;
            }

            this.UpdateWidthAndHeight();
            this.ApplyLayout();
            base.Update();
        }

        protected override void UpdateWidthAndHeight()
        {
            var width = Scale(30) + (this.titleLabel.Text.Length * UIStatics.TextRenderer.LetterSpace);
            foreach (var label in labels)
            {
                var w = Scale(30) + (label.Text.Length * UIStatics.TextRenderer.LetterSpace);
                if (w > width) width = w;
            }

            this.H = Scale(24) + (labels.Count * UIStatics.TextRenderer.LineHeight);
            this.W = width;
        }
    }
}
