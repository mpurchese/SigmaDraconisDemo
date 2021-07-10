namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Microsoft.Xna.Framework;
    using Language;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A simple tooltip with title text, automatic width, and optionally some text
    /// </summary>
    public class SimpleTooltip : Tooltip
    {
        protected List<TextLabel> descriptionLabels = new List<TextLabel>();
        protected string prevText = "";
        protected int lineCount = 0;
        protected int textWidth = 0;
        private Color textColour;
        private TextAlignment textAlignment;

        private readonly bool isLanguageConfigured;
        private readonly object titleId;

        public Color TextColour 
        {
            get => this.textColour;
            set
            {
                if (this.textColour != value)
                {
                    this.textColour = value;
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public TextAlignment TextAlignment
        {
            get => this.textAlignment;
            set
            {
                if (this.textAlignment != value)
                {
                    this.textAlignment = value;
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public SimpleTooltip(IUIElement parent, IUIElement attachedElement, string title = "", string text = "", TextAlignment textAlignment = TextAlignment.TopCentre)
            : base(parent, attachedElement, 8, 15, "")
        {
            this.SetTitle(title);
            this.textColour = UIColour.DefaultText;
            this.textAlignment = textAlignment;
            if (text != "") this.SetText(text);
        }

        public SimpleTooltip(IUIElement parent, IUIElement attachedElement, object titleId)
            : base(parent, attachedElement, 8, 15, "")
        {
            this.isLanguageConfigured = true;
            this.titleId = titleId;
            this.SetTitle(LanguageManager.Get(titleId.GetType(), titleId));
            this.textColour = UIColour.DefaultText;
        }

        public override void SetTitle(string text)
        {
            if (text == this.titleLabel.Text) return;

            this.textWidth = this.descriptionLabels.Any() 
                ? Math.Max(text.Length, this.descriptionLabels.Select(l => l.Text.Length).Max()) 
                : text.Length;

            base.SetTitle(text);
        }

        public override void ApplyScale()
        {
            this.UpdateWidthAndHeight();
            this.IsContentChangedSinceDraw = true;
        }

        public override void ApplyLayout()
        {
            var y = Scale(this.titleLabel.Text == "" ? 4 : 18);
            var haveText = false;
            foreach (var label in this.descriptionLabels)
            {
                if (!haveText && label.Text == "") continue;

                haveText = true;
                label.W = this.W;
                label.H = Scale(14);
                label.Y = y;
                y += 16 * UIStatics.Scale / 100;
            }

            base.ApplyLayout();
        }

        public void SetText(string description)
        {
            this.BuildLabels(description);
            this.ApplyLayout();
        }

        protected override void HandleLanguageChange()
        {
            if (this.isLanguageConfigured) this.SetTitle(LanguageManager.Get(this.titleId.GetType(), this.titleId));
            base.HandleLanguageChange();
        }

        protected virtual void BuildLabels(string description)
        {
            if (description == null) description = "";

            if (this.prevText == description) return;
            this.prevText = description;

            var descriptionLines = description.Split('|');
            this.textWidth = this.titleLabel.Text.Length;

            foreach (var label in this.descriptionLabels) label.Text = "";

            if (string.IsNullOrEmpty(description))
            {
                this.lineCount = 0;
                this.UpdateWidthAndHeight();
                return;
            }

            foreach (var line in descriptionLines)
            {
                if (line.Length > this.textWidth) this.textWidth = line.Length;
            }

            this.lineCount = descriptionLines.Length;
            this.UpdateWidthAndHeight();

            var lineNumber = 0;
            foreach (var line in descriptionLines)
            {
                if (descriptionLabels.Count > lineNumber)
                {
                    this.descriptionLabels[lineNumber].Text = line;
                    this.descriptionLabels[lineNumber].Colour = this.GetColourForLine(lineNumber + 1);
                }
                else
                {
                    var x = this.textAlignment == TextAlignment.TopLeft ? Scale(10) : 0;
                    var w = this.textAlignment == TextAlignment.TopLeft ? this.W - Scale(10) : this.W;
                    var label = new TextLabel(this.textRenderer, this, x, Scale(this.titleLabel.Text == "" ? 4 : 18) + (lineNumber * this.textRenderer.LineHeight), w, Scale(20), line, this.GetColourForLine(lineNumber + 1), this.textAlignment);
                    this.descriptionLabels.Add(label);
                    this.AddChild(label);
                }

                lineNumber++;
            }
        }

        protected virtual Color GetColourForLine(int lineNumber)
        {
            return this.textColour;
        }

        protected override void UpdatePostion()
        {
            this.X = Math.Min(UIStatics.CurrentMouseState.X + Scale(16), GameScreen.Instance.W - this.W - 1);
            this.Y = Math.Min(UIStatics.CurrentMouseState.Y + Scale(28), GameScreen.Instance.H - this.H - 1);
        }

        protected override void UpdateWidthAndHeight()
        {
            if (this.lineCount > 0)
            {
                this.H = Scale(this.titleLabel.Text == "" ? 8 : 24) + (this.lineCount * 16 * UIStatics.Scale / 100);
                this.W = Scale(20) + (this.textWidth * (7 * UIStatics.Scale / 100));
            }
            else
            {
                this.H = Scale(16);
                this.W = Scale(8) + (this.titleLabel.Text.Length * (7 * UIStatics.Scale / 100));
            }
        }
    }
}
