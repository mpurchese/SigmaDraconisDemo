namespace SigmaDraconis.UI
{
    using System;
    using Draconis.UI;
    using Language;
    using Shared;
    using World;
    using WorldInterfaces;

    public class PlantPanel : DeconstructableThingPanel
    {
        protected readonly TextLabel descriptionLabel1;
        protected readonly TextLabel descriptionLabel2;
        protected readonly TextLabel descriptionLabel3;
        protected readonly TextLabel descriptionLabel1Coloured;
        protected readonly TextLabel descriptionLabel2Coloured;
        protected readonly TextLabel descriptionLabel3Coloured;
        protected readonly WorkPriorityTextButton harvestButton;
        private int prevFruitCount = 0;
        private ThingType prevThingType;

        public PlantPanel(IUIElement parent, int y) : base(parent, y)
        {
            this.harvestButton = new WorkPriorityTextButton(this, Scale(60), Scale(50), Scale(200), "") { IsEnabled = false };
            this.harvestButton.PriorityChanged += this.OnHarvestPriorityChanged;
            this.AddChild(this.harvestButton);

            this.descriptionLabel1 = UIHelper.AddTextLabel(this, 0, 74, 320, UIColour.DefaultText);
            this.descriptionLabel2 = UIHelper.AddTextLabel(this, 0, 90, 320, UIColour.DefaultText);
            this.descriptionLabel3 = UIHelper.AddTextLabel(this, 0, 106, 320, UIColour.DefaultText);
            this.descriptionLabel1Coloured = UIHelper.AddTextLabel(this, 0, 74, 320, UIColour.DefaultText);
            this.descriptionLabel2Coloured = UIHelper.AddTextLabel(this, 0, 90, 320, UIColour.DefaultText);
            this.descriptionLabel3Coloured = UIHelper.AddTextLabel(this, 0, 106, 320, UIColour.DefaultText);

            this.H = Scale(130);
        }

        public override void Update()
        {
            if (this.IsVisible)
            {
                if (this.thing.IsRecycling)
                {
                    this.generalInfoLabel.Y = Scale(24);
                    this.generalInfoLabel.Text = LanguageManager.Get<StringsForThingPanels>(StringsForThingPanels.UnderDeconstruction, this.thing.RecycleProgress);
                    this.deconstructButton.IsVisible = false;
                    this.harvestButton.IsVisible = false;
                    this.descriptionLabel1.IsVisible = false;
                    this.descriptionLabel2.IsVisible = false;
                    this.descriptionLabel3.IsVisible = false;
                    this.descriptionLabel1Coloured.IsVisible = false;
                    this.descriptionLabel2Coloured.IsVisible = false;
                    this.descriptionLabel3Coloured.IsVisible = false;
                }
                else
                {
                    this.deconstructButton.IsVisible = true;
                    this.descriptionLabel1.IsVisible = true;
                    this.descriptionLabel2.IsVisible = true;
                    this.descriptionLabel3.IsVisible = true;
                    this.descriptionLabel1Coloured.IsVisible = true;
                    this.descriptionLabel2Coloured.IsVisible = true;
                    this.descriptionLabel3Coloured.IsVisible = true;

                    if (this.thing.ThingType != this.prevThingType)
                    {
                        this.UpdateDescription();
                        this.prevThingType = this.thing.ThingType;
                    }

                    var yield = this.thing.GetDeconstructionYield();
                    var noYield = true;
                    foreach (var kv in yield)
                    {
                        if (kv.Value > 0)
                        {
                            var deconstructStr = LanguageManager.Get<StringsForThingPanels>(StringsForThingPanels.Deconstruct);
                            var itemTypeStr = LanguageManager.Get<ItemType>(kv.Key);
                            this.deconstructButton.Text = $"{deconstructStr}: {kv.Value} {itemTypeStr}";
                            noYield = false;
                            break;
                        }
                    }

                    if (noYield) this.deconstructButton.IsVisible = false;
                    else this.deconstructButton.PriorityLevel = this.thing.RecyclePriority;

                    if (this.thing is IFruitPlant plant && plant.CountFruitAvailable > 0)
                    {
                        this.harvestButton.IsVisible = true;
                        this.harvestButton.PriorityLevel = plant.HarvestFruitPriority;

                        var fruitAvailable = plant.CountFruitAvailable;
                        if (fruitAvailable > 0 && fruitAvailable != this.prevFruitCount)
                        {
                            var harvestStr = LanguageManager.Get<StringsForThingPanels>(StringsForThingPanels.Harvest);
                            var itemTypeStr = LanguageManager.Get<ItemType>(ItemType.Fruit);
                            this.harvestButton.Text = $"{harvestStr}: {plant.CountFruitAvailable} {itemTypeStr}";
                            this.prevFruitCount = fruitAvailable;
                        }

                        this.generalInfoLabel.Y = this.harvestButton.Y + Scale(26);
                        this.generalInfoLabel.Text = World.CanHarvestFruit ? "" : LanguageManager.Get<StringsForThingPanels>(StringsForThingPanels.HarvestFruitRequirement);
                        this.harvestButton.IsEnabled = World.CanHarvestFruit;
                    }
                    else
                    {
                        if (this.harvestButton.IsVisible) this.IsContentChangedSinceDraw = true;
                        this.harvestButton.IsVisible = false;
                        this.generalInfoLabel.Text = "";
                    }
                }
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            if (this.thing != null) this.UpdateDescription();
            base.HandleLanguageChange();
        }

        private void UpdateDescription()
        {
            var descriptionParts = this.thing.Description.Split('|');
            this.ProcessDescriptionPart(this.descriptionLabel1, this.descriptionLabel1Coloured, descriptionParts.Length > 0 ? descriptionParts[0] : "");
            this.ProcessDescriptionPart(this.descriptionLabel2, this.descriptionLabel2Coloured, descriptionParts.Length > 1 ? descriptionParts[1] : "");
            this.ProcessDescriptionPart(this.descriptionLabel3, this.descriptionLabel3Coloured, descriptionParts.Length > 2 ? descriptionParts[2] : "");
        }

        private void ProcessDescriptionPart(TextLabel label1, TextLabel label2, string text)
        {
            if (text == "")
            {
                label1.IsVisible = false;
                label2.IsVisible = false;
            }

            var colour = UIColour.YellowText;
            var colourIndexStart = text.IndexOf("[yellow]");
            var colourIndexEnd = -1;

            if (colourIndexStart >= 0)
            {
                text = text.Replace("[yellow]", "");
                colourIndexEnd = text.IndexOf("[/yellow]");
                text = text.Replace("[/yellow]", "");
            }
            else
            {
                colourIndexStart = text.IndexOf("[red]");
                if (colourIndexStart >= 0)
                {
                    text = text.Replace("[red]", "");
                    colourIndexEnd = text.IndexOf("[/red]");
                    text = text.Replace("[/red]", "");
                    colour = UIColour.RedText;
                }
                else
                {
                    colourIndexStart = text.IndexOf("[blue]");
                    if (colourIndexStart >= 0)
                    {
                        text = text.Replace("[blue]", "");
                        colourIndexEnd = text.IndexOf("[/blue]");
                        text = text.Replace("[/blue]", "");
                        colour = UIColour.LightBlueText;
                    }
                    else
                    {
                        colourIndexStart = text.IndexOf("[pink]");
                        if (colourIndexStart >= 0)
                        {
                            text = text.Replace("[pink]", "");
                            colourIndexEnd = text.IndexOf("[/pink]");
                            text = text.Replace("[/pink]", "");
                            colour = UIColour.PinkText;
                        }
                        else
                        {
                            colourIndexStart = text.IndexOf("[orange]");
                            if (colourIndexStart >= 0)
                            {
                                text = text.Replace("[orange]", "");
                                colourIndexEnd = text.IndexOf("[/orange]");
                                text = text.Replace("[/orange]", "");
                                colour = UIColour.OrangeText;
                            }
                        }
                    }
                }
            }

            if (colourIndexStart >= 0 && colourIndexEnd >= 0)
            {
                label2.Text = new String(' ', colourIndexStart) + text.Substring(colourIndexStart, colourIndexEnd - colourIndexStart) + new String(' ', text.Length - colourIndexEnd);
                label2.Colour = colour;
            }
            else label2.Text = "";

            label1.Text = text;
        }

        protected void OnHarvestPriorityChanged(object sender, EventArgs e)
        {
            if (!(this.thing is IFruitPlant plant)) return;

            if (this.harvestButton.PriorityLevel == WorkPriority.Disabled || plant.HarvestFruitPriority == WorkPriority.Disabled)
            {
                // TODO
            }

            plant.SetHarvestFruitPriority(this.harvestButton.PriorityLevel);
        }
    }
}
