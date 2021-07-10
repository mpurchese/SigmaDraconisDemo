namespace Draconis.UI
{
    using Draconis.Shared;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    
    public class TextArea : RenderTargetElement
    {
        private readonly VerticalScrollBar scrollBar;
        private readonly List<TextLabel> labels = new List<TextLabel>();
        private Color colour;
        private string text = "";

        public TextArea(IUIElement parent, int x, int y, int width, int height, Color colour) : base(parent, x, y, width, height, true)
        {
            this.colour = colour;
            this.backgroundColour = new Color(0, 0, 0, 32);

            this.scrollBar = new VerticalScrollBar(this, this.W - Scale(20), 0, this.H, 8) { ScrollSpeed = 2, IsVisible = false };
            this.AddChild(this.scrollBar);
            this.scrollBar.ScrollPositionChange += this.ScrollPositionChange;

            this.SetText(text);
        }

        public TextArea(IUIElement parent, int x, int y, int width, int height, Color colour, Color backgroundColour, bool hasBorder = false) : base(parent, x, y, width, height, hasBorder)
        {
            this.colour = colour;
            this.backgroundColour = backgroundColour;

            this.scrollBar = new VerticalScrollBar(this, this.W - Scale(20), 0, this.H, 8) { ScrollSpeed = 2, IsVisible = false };
            this.AddChild(this.scrollBar);
            this.scrollBar.ScrollPositionChange += this.ScrollPositionChange;

            this.SetText(text);
        }

        private void ScrollPositionChange(object sender, EventArgs e)
        {
            this.SetText(this.text, true, false);
        }

        /// <summary>
        /// Gets the required unscaled size that will allow all text to fit without scrolling.  Does not add any padding.
        /// </summary>
        public static Vector2i GetRequiredSize(string text)
        {
            var lines = text.Split('|');

            return new Vector2i(
                lines.Max(l => l.Length * 7),
                lines.Sum(l => string.IsNullOrWhiteSpace(l) ? 10 : 16));
        }

        public void SetText(string text, bool forceUpdate = false, bool resetScroll = true)
        {
            if (!forceUpdate && text == this.text) return;
            this.text = text;

            if (resetScroll) this.scrollBar.ScrollPosition = 0;

            var lines = text.Split('|');
            var totalHeight = lines.Sum(l => string.IsNullOrWhiteSpace(l) ? 10 : 16);

            var unscaledH = UnScale(this.H);
            if (unscaledH < totalHeight)
            {
                this.scrollBar.IsVisible = true;
                this.scrollBar.PageSize = unscaledH;
                this.scrollBar.FractionVisible = unscaledH / (float)totalHeight;
                this.scrollBar.MaxScrollPosition = totalHeight - unscaledH;
            }
            else this.scrollBar.IsVisible = false;

            var paddingTop = Math.Max((unscaledH - totalHeight) / 3, 1);

            var y = paddingTop - this.scrollBar.ScrollPosition;
            var w = this.scrollBar.IsVisible ? this.scrollBar.X : this.W;
            var labelIndex = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    y += 10;
                    continue;
                }

                if (labelIndex >= this.labels.Count)
                {
                    var label = new TextLabel(this, 0, Scale(y), w, Scale(18), line, this.colour);
                    this.AddChild(label);
                    this.labels.Add(label);
                }
                else
                {
                    var label = this.labels[labelIndex];
                    label.Y = Scale(y);
                    label.W = w;
                    label.H = Scale(18);
                    label.Text = line;
                    label.ApplyLayout();
                }

                labelIndex++;
                y += 16;
            }

            for (int i = labelIndex; i < this.labels.Count; i++) this.labels[i].Text = "";
        }

        public override void ApplyLayout()
        {
            this.scrollBar.ApplyScale();
            this.scrollBar.ApplyLayout();
            this.scrollBar.X = this.W - Scale(20);
            this.appliedScale = UIStatics.Scale;

            this.SetText(text, true);

            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        public void SetLastLineColour(Color colour)
        {
            if (this.labels.Count == 0) return;

            var lastLine = this.labels.FindLastIndex(l => l.Text != "");

            for (int i = 0; i < this.labels.Count; i++) this.labels[i].Colour = (i == lastLine) ? colour : this.colour;
        }
    }
}
