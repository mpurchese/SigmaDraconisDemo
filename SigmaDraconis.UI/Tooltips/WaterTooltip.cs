namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.UI;
    using Language;
    using Shared;
    using World;
    using WorldInterfaces;

    public class WaterTooltip : Tooltip
    {
        private readonly List<TextLabel> labels1 = new List<TextLabel>();
        private readonly List<TextLabel> labels2 = new List<TextLabel>();
        private int updateTimer = 0;
        private string waterGenFormat;
        private string waterUseFormat;

        public WaterTooltip(IUIElement parent, IUIElement attachedElement)
            : base(parent, attachedElement, Scale(240), Scale(76), GetString(StringsForWaterDisplay.TooltipTitle))
        {
            this.waterGenFormat = GetString(StringsForWaterDisplay.WaterGenFormat);
            this.waterUseFormat = GetString(StringsForWaterDisplay.WaterUseFormat);
        }

        public override void Update()
        {
            if (!this.attachedElement.IsMouseOver || !this.IsEnabled || World.ResourceNetwork == null)
            {
                this.isCurrentlyVisible = false;
                return;
            }

            if (this.updateTimer > 0)
            {
                this.updateTimer--;
                if (this.updateTimer > 0)
                {
                    base.Update();
                    return;
                }
            }

            var consumerValues = new Dictionary<ThingType, decimal>();
            var generatorValues = new Dictionary<ThingType, decimal>();

            var consumerCounts = new Dictionary<ThingType, int>();
            var generatorCounts = new Dictionary<ThingType, int>();

            foreach (var consumer in World.ResourceNetwork.WaterConsumers)
            {
                var rate = consumer is IWaterConsumer p ? (p.WaterUseRate / 100M) : 36M;
                if (!consumerValues.ContainsKey(consumer.ThingType))
                {
                    consumerValues.Add(consumer.ThingType, rate);
                    consumerCounts.Add(consumer.ThingType, 1);
                }
                else
                {
                    consumerValues[consumer.ThingType] += rate;
                    consumerCounts[consumer.ThingType]++;
                }
            }

            foreach (var generator in World.ResourceNetwork.WaterGenerators)
            {
                if (!generatorValues.ContainsKey(generator.ThingType))
                {
                    generatorValues.Add(generator.ThingType, generator.WaterGenRate / 100M);
                    generatorCounts.Add(generator.ThingType, 1);
                }
                else
                {
                    generatorValues[generator.ThingType] += generator.WaterGenRate / 100M;
                    generatorCounts[generator.ThingType]++;
                }
            }

            int row = 0;
            var textWidth = this.titleLabel.Text.Length;

            foreach (var m in generatorValues.OrderByDescending(v => v.Value))
            {
                var name = LanguageManager.GetName(m.Key, generatorCounts[m.Key] > 1);

                if (row + 1 > labels1.Count)
                {
                    var label1 = new TextLabel(this, 0, 0, this.W - Scale(72), Scale(14), $"{name} :", UIColour.DefaultText, TextAlignment.TopRight);
                    this.AddChild(label1);
                    this.labels1.Add(label1);

                    var label2 = new TextLabel(this, this.W - Scale(66), 0, Scale(40), Scale(14), string.Format(this.waterGenFormat, m.Value), UIColour.GreenText, TextAlignment.TopLeft);
                    this.AddChild(label2);
                    this.labels2.Add(label2);
                }
                else
                {
                    this.labels1[row].Text = $"{name} :";
                    this.labels2[row].Text = string.Format(this.waterGenFormat, m.Value);
                    this.labels2[row].Colour = UIColour.GreenText;
                }

                textWidth = Math.Max(textWidth, this.labels1[row].Text.Length + this.labels2[row].Text.Length + 1);
                row++;
            }

            foreach (var m in consumerValues.OrderBy(v => v.Value))
            {
                var name = LanguageManager.GetName(m.Key, consumerCounts[m.Key] > 1);

                if (row + 1 > labels1.Count)
                {
                    var label1 = new TextLabel(this.textRenderer, this, 0, 0, this.W - Scale(72), Scale(14), $"{name} :", UIColour.DefaultText, TextAlignment.TopRight);
                    this.AddChild(label1);
                    this.labels1.Add(label1);

                    var label2 = new TextLabel(this.textRenderer, this, this.W - Scale(66), 0, Scale(40), Scale(14), string.Format(this.waterUseFormat, m.Value), UIColour.DarkRedText, TextAlignment.TopLeft);
                    this.AddChild(label2);
                    this.labels2.Add(label2);
                }
                else
                {
                    this.labels1[row].Text = $"{name} :";
                    this.labels2[row].Text = string.Format(this.waterUseFormat, m.Value);
                    this.labels2[row].Colour = UIColour.DarkRedText;
                }

                textWidth = Math.Max(textWidth, this.labels1[row].Text.Length + this.labels2[row].Text.Length + 1);
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

            this.updateTimer = 7;   // Update 10x per second

            var y = Scale(18);
            foreach (var label in this.labels1)
            {
                label.W = this.W - Scale(72);
                label.Y = y;
                y += Scale(16);
            }

            y = Scale(18);
            foreach (var label in this.labels2)
            {
                label.X = this.W - Scale(66);
                label.Y = y;
                y += Scale(16);
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            this.titleLabel.Text = GetString(StringsForWaterDisplay.TooltipTitle);
            this.waterGenFormat = GetString(StringsForWaterDisplay.WaterGenFormat);
            this.waterUseFormat = GetString(StringsForWaterDisplay.WaterUseFormat);

            base.HandleLanguageChange();
        }

        protected static string GetString(StringsForWaterDisplay key)
        {
            return LanguageManager.Get<StringsForWaterDisplay>(key);
        }
    }
}
