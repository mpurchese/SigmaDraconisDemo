namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using World;
    using WorldControllers;
    using WorldInterfaces;

    public class DebugDialog : Dialog
    {
        private readonly LeftRightPicker plantPicker;
        private readonly TextButton closeButton;
        private readonly TextLabel seedsLabel;
        private readonly TextLabel flowersLabel;
        private readonly TextLabel fruitLabel1;
        private readonly TextLabel fruitLabel2;
        private readonly List<TextLabel> growthStageLabels = new List<TextLabel>();
        private readonly TextLabel deadLabel;
        private Texture2D pixelTexture2;

        public event EventHandler<EventArgs> Close;

        public DebugDialog(IUIElement parent)
            : base(parent, Scale(320), Scale(400), "DEBUG - PLANT GROWTH")
        {
            this.IsVisible = false;

            var thingTypes = new List<ThingType> {
                ThingType.Bush,
                ThingType.Grass,
                ThingType.CoastGrass,
                ThingType.SmallPlant1,
                ThingType.SmallPlant2,
                ThingType.SmallPlant3,
                ThingType.SmallPlant4,
                ThingType.SmallPlant5,
                ThingType.SmallPlant6,
                ThingType.SmallPlant7,
                ThingType.SmallPlant8 };

            var names = new List<string>();
            names.AddRange(thingTypes.Select(t => LanguageManager.GetName(t)));

            this.plantPicker = new LeftRightPicker(this, (this.W / 2) - Scale(140), Scale(24), Scale(280), names, 0) { Tags = thingTypes.OfType<object>().ToList() };
            this.AddChild(this.plantPicker);

            this.closeButton = new TextButton(this, (this.W / 2) - Scale(50), this.H - Scale(28), Scale(100), Scale(20), "CLOSE");
            this.closeButton.MouseLeftClick += this.OnCloseClick;
            this.AddChild(this.closeButton);

            this.seedsLabel = new TextLabel(this, Scale(36), Scale(48), "", UIColour.DefaultText);

            var y = 66;
            for (int i = 1; i <= 12; i++)
            {
                var label = new TextLabel(this, Scale(36), Scale(y), $"", UIColour.DefaultText);
                this.growthStageLabels.Add(label);
                this.AddChild(label);
                y += 18;
            }

            this.flowersLabel = new TextLabel(this, Scale(36), Scale(y), "", UIColour.DefaultText);
            this.fruitLabel1 = new TextLabel(this, Scale(36), Scale(y + 18), "", UIColour.DefaultText);
            this.fruitLabel2 = new TextLabel(this, Scale(36), Scale(y + 36), "", UIColour.DefaultText);
            this.deadLabel = new TextLabel(this, Scale(36), Scale(y + 54), "", UIColour.DefaultText);

            this.AddChild(this.seedsLabel);
            this.AddChild(this.flowersLabel);
            this.AddChild(this.fruitLabel1);
            this.AddChild(this.fruitLabel2);
            this.AddChild(this.deadLabel);

            this.UpdateHorizontalPosition();
            this.UpdateVerticalPosition();
        }

        public override void Update()
        {
            if (this.IsVisible && this.Parent is ModalBackgroundBox && this.backgroundColour.A != UIStatics.BackgroundAlpha)
            {
                this.backgroundColour = new Color(0, 0, 0, UIStatics.BackgroundAlpha);
                this.IsContentChangedSinceDraw = true;
            }

            base.Update();
            if (!this.IsVisible) return;

            var thingType = (ThingType)this.plantPicker.SelectedTag;

            if (thingType == ThingType.Grass || thingType == ThingType.CoastGrass) this.seedsLabel.Text = $"Seeds: N/A";
            else
            {
                var seedCount = PlantGrowthController.SeedQueue.Count(s => s.ThingType == thingType);
                this.seedsLabel.Text = $"Seeds: {seedCount}";
            }

            var plants = World.GetThings<IPlant>(thingType);
            if (plants.Any())
            {
                if (plants.First().CanFlower)
                {
                    this.flowersLabel.Text = $"Flowering: {plants.Count(p => p.IsFlowering)}";
                }
                else this.flowersLabel.Text = "Flowering: N/A";

                if (plants.First() is IFruitPlant fp)
                {
                    if (fp.CanFruitUnripe) this.fruitLabel1.Text = $"Fruiting (unripe): {plants.OfType<IFruitPlant>().Count(p => p.HasFruitUnripe)}";
                    else this.fruitLabel1.Text = "Fruiting (unripe): N/A";
                    this.fruitLabel2.Text = $"Fruiting (ripe): {plants.OfType<IFruitPlant>().Count(p => p.CountFruitAvailable > 0)}";
                }
                else
                {
                    this.fruitLabel1.Text = "Fruiting (unripe): N/A";
                    this.fruitLabel2.Text = "Fruiting (ripe): N/A";
                }

                var maxGrowthStage = plants.First().MaxGrowthStage;
                var lookup = plants.ToLookup(p => p.GrowthStage, p => p);
                for (int i = 1; i <= 12; i++)
                {
                    if (i <= maxGrowthStage)
                    {
                        var count = lookup.Contains(i) ? lookup[i].Count() : 0;
                        this.growthStageLabels[i - 1].Text = $"Growth Stage {i}: {count}";
                    }
                    else this.growthStageLabels[i - 1].Text = $"Growth Stage {i}: N/A";
                }

                if (plants.First().HasDeadFrame) this.deadLabel.Text = $"Dead: {plants.Count(p => p.IsDead)}";
                else this.deadLabel.Text = "Dead: N/A";
            }
            else
            {
                this.flowersLabel.Text = "";
                this.fruitLabel1.Text = "";
                this.fruitLabel2.Text = "";
                this.deadLabel.Text = "";
                for (int i = 0; i < this.growthStageLabels.Count; i++) this.growthStageLabels[i].Text = "";
            }
        }

        private void OnCloseClick(object sender, MouseEventArgs e)
        {
            this.IsVisible = false;
            this.Close?.Invoke(this, new EventArgs());
        }

        public override void LoadContent()
        {
            this.pixelTexture2 = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] color2 = new Color[1] { new Color(0, 0, 0, 128) };
            this.pixelTexture2.SetData(color2);

            base.LoadContent();
        }

        protected override void DrawBaseLayer()
        {
            if (this.IsVisible)
            {
                Rectangle r1 = new Rectangle(0, 0, this.W, Scale(14));
                Rectangle r2 = new Rectangle(Scale(12), Scale(20), this.W - Scale(24), this.H - Scale(54));

                spriteBatch.Begin();
                spriteBatch.Draw(pixelTexture, r1, Color.White);
                spriteBatch.Draw(pixelTexture2, r2, Color.White);
                spriteBatch.End();
            }
        }
    }
}
