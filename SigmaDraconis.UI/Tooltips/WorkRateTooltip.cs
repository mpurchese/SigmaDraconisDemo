namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.UI;
    using Language;

    public class WorkRateTooltip : Tooltip
    {
        protected List<TextLabel> labels1 = new List<TextLabel>();
        protected List<TextLabel> labels2 = new List<TextLabel>();

        protected Dictionary<string, int> modifiers = new Dictionary<string, int>();
        protected bool isInvalidated;
        protected int scale;

        public WorkRateTooltip(IUIElement parent, IUIElement attachedElement)
            : base(parent, attachedElement, Scale(160), Scale(76), LanguageManager.Get<StringsForThingPanels>(StringsForThingPanels.WorkRate))
        {
        }

        public void UpdateModifiers(Dictionary<string, int> newModifiers)
        {
            if (newModifiers.Count != this.modifiers.Count)
            {
                this.isInvalidated = true;
            }
            else
            {
                foreach (var m in newModifiers)
                {
                    if (!this.modifiers.ContainsKey(m.Key) || this.modifiers[m.Key] != m.Value)
                    {
                        this.isInvalidated = true;
                        break;
                    }
                }
            }

            if (this.isInvalidated)
            {
                this.modifiers.Clear();
                foreach (var m in newModifiers) this.modifiers.Add(m.Key, m.Value);
            }
        }

        public override void Update()
        {
            if (this.isInvalidated || UIStatics.Scale != this.scale)
            {
                this.scale = UIStatics.Scale;
                int row = 0;
                var textWidth = this.titleLabel.Text.Length;
                var textWidthRight = 1;
                foreach (var m in this.modifiers.OrderByDescending(kv => kv.Value))
                {
                    var sign = m.Value >= 100 ? "+" : "-";
                    var absValue = Math.Abs(m.Value - 100);
                    var colour = m.Value >= 100 ? UIColour.GreenText : UIColour.RedText;

                    if (row + 1 > labels1.Count)
                    {
                        var label1 = new TextLabel(this, 0, 0, this.W - Scale(72), Scale(14), $"{m.Key} :", UIColour.DefaultText, TextAlignment.TopRight);
                        this.AddChild(label1);
                        this.labels1.Add(label1);

                        var label2 = new TextLabel(this, this.W - Scale(66), 0, Scale(40), Scale(14), $"{sign}{absValue}%", colour, TextAlignment.TopLeft);
                        this.AddChild(label2);
                        this.labels2.Add(label2);
                    }
                    else
                    {
                        this.labels1[row].Text = $"{m.Key} :";
                        this.labels2[row].Text = $"{sign}{absValue}%";
                        this.labels2[row].Colour = colour;
                    }

                    textWidth = Math.Max(textWidth, this.labels1[row].Text.Length + this.labels2[row].Text.Length + 1);
                    textWidthRight = Math.Max(textWidthRight, this.labels2[row].Text.Length + 1);
                    row++;
                }

                for (int r = row; r < this.labels1.Count; r++)
                {
                    this.labels1[r].Text = "";
                    this.labels2[r].Text = "";
                }

                this.H = Scale(24) + (row * Scale(16));
                this.W = Scale(32) + (textWidth * (7 * UIStatics.Scale / 100));
                this.titleLabel.W = this.W;

                var offsetR = textWidthRight * 7;

                var y = Scale(18);
                foreach (var label in this.labels1)
                {
                    label.W = this.W - Scale(offsetR + 12);
                    label.Y = y;
                    y += Scale(16);
                }

                y = Scale(18);
                foreach (var label in this.labels2)
                {
                    label.X = this.W - Scale(offsetR + 6);
                    label.Y = y;
                    y += Scale(16);
                }

                this.isInvalidated = false;
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            this.titleLabel.Text = LanguageManager.Get<StringsForThingPanels>(StringsForThingPanels.WorkRate);
            base.HandleLanguageChange();
        }
    }
}
