namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Draconis.UI;
    using Config;
    using Language;
    using Settings;
    using Shared;

    public class BlueprintTooltip : Tooltip
    {
        private readonly Icon energyIcon = null;
        private readonly Icon timeIcon = null;
        private readonly Icon itemsIcon = null;
        protected TextLabel energyAmountLabel = null;
        protected TextLabel timeAmountLabel = null;
        protected List<TextLabel> descriptionLabels = new List<TextLabel>();
        protected TextLabel lockedReasonLabel = null;
        protected TextLabel prefabLabel = null;
        protected int prefabCount = 0;
        protected ThingTypeDefinition thingTypeDefinition;
        protected List<KeyValuePair<ItemType, int>> resourceCosts = new List<KeyValuePair<ItemType, int>>();
        protected Dictionary<ItemType, TextLabel> resourceLabels;

        private static int LetterSpace => 7 * UIStatics.Scale / 100;
        private static int LineHeight => 16 * UIStatics.Scale / 100;

        private static readonly Dictionary<ItemType, int> resourceOrder 
            = new Dictionary<ItemType, int> { { ItemType.Metal, 1 }, { ItemType.Stone, 2 }, { ItemType.Glass, 3 }, { ItemType.Composites, 4 }, { ItemType.BatteryCells, 5 }, { ItemType.SolarCells, 6 }, { ItemType.Compost, 7 } };

        public BlueprintTooltip(IUIElement parent, IUIElement attachedElement, ThingType thingType, ThingTypeDefinition definition, string title, int prefabCount = 0, string lockedDescription = "")
            : base(parent, attachedElement, Scale(380), Scale(76), title)
        {
            this.energyIcon = new Icon("Textures\\Icons\\Energy");
            this.timeIcon = new Icon("Textures\\Icons\\Time");
            this.itemsIcon = new Icon("Textures\\Icons\\Items", 13);

            this.thingTypeDefinition = definition;

            var description = LanguageManager.GetDescription(thingType);

            var descriptionLines = new List<string>();
            var colours = new List<Color>();

            if (definition.IsLaunchPadRequired)
            {
                descriptionLines.Add(GetString(StringsForThingPanels.BuildOnLaunchPad));
                descriptionLines.Add("");
                colours.Add(UIColour.YellowText);
                colours.Add(UIColour.DefaultText);
            }
            else if (definition.IsRocketGantryRequired)
            {
                descriptionLines.Add(GetString(StringsForThingPanels.BuildInRocketGantry));
                descriptionLines.Add(GetString(StringsForThingPanels.RocketRequiresHydrogen, Constants.RocketFuelToLaunch));
                descriptionLines.Add("");
                colours.Add(UIColour.YellowText);
                colours.Add(UIColour.YellowText);
                colours.Add(UIColour.DefaultText);
            }


            descriptionLines.AddRange(description.Split('|'));
            colours.AddRange(descriptionLines.Select(d => UIColour.DefaultText));

            if (definition.IsCookerRequired)
            {
                descriptionLines.Add(GetString(StringsForThingPanels.CookerRequiredToHarvest));
                colours.Add(UIColour.YellowText);
            }

            if (definition.IsTableRequired)
            {
                descriptionLines.Add(GetString(StringsForThingPanels.TableRequiredForKek));
                colours.Add(UIColour.YellowText);
            }

            if (definition.MinTemperature > -99)
            {
                descriptionLines.Add($"{GetString(StringsForThingPanels.MinOperatingTemperature)}: {LanguageHelper.FormatTemperature(definition.MinTemperature)}");
                colours.Add(UIColour.YellowText);
            }

            // Keyboard shortcut
            var thingTypeName = Enum.GetName(typeof(ThingType), thingType);
            var key = SettingsManager.GetKeysForAction($"Build:{thingTypeName}").FirstOrDefault();
            if (key != null)
            {
                descriptionLines.Add($"{GetString(StringsForThingPanels.Shortcut)}: {key}");
                colours.Add(UIColour.DefaultText);
            }

            this.resourceCosts.AddRange(definition.ConstructionCosts.Where(kv => kv.Value > 0).OrderBy(kv => resourceOrder[kv.Key]));
            if (this.resourceCosts.Any())
            {
                this.energyAmountLabel = new TextLabel(this, 0, 0, $"{definition.EnergyCost.KWh}", UIColour.DefaultText);
                this.timeAmountLabel = new TextLabel(this, 0, 0, $"{definition.ConstructionTimeMinutes}", UIColour.DefaultText);

                this.resourceLabels = this.resourceCosts.ToDictionary(kv => kv.Key, kv => new TextLabel(this, 0, 0, kv.Value.ToString(), UIColour.DefaultText));

                foreach (var label in this.resourceLabels.Values) this.AddChild(label);
                this.AddChild(this.energyAmountLabel);
                this.AddChild(this.timeAmountLabel);
            }

            var y = Scale(this.resourceCosts.Any() ? 50 : 20);
            for (var i = 0; i < descriptionLines.Count; i++)
            {
                var label = new TextLabel(this, 0, y, this.W, Scale(20), descriptionLines[i], colours[i]);
                this.descriptionLabels.Add(label);
                this.AddChild(label);
                var w = Scale(20) + (LineHeight * descriptionLines[i].Length);
                if (w > this.W) this.W = w;
                y += LineHeight;
            }

            this.lockedReasonLabel = new TextLabel(this, 0, y + Scale(18), this.W, Scale(20), lockedDescription, UIColour.RedText) { IsVisible = !string.IsNullOrEmpty(lockedDescription) };
            this.AddChild(this.lockedReasonLabel);

            var prefabDescription = prefabCount > 0 ? $"{GetString(StringsForThingPanels.PrefabsAvailable)}: {prefabCount}" : "";
            this.prefabCount = prefabCount;
            this.prefabLabel = new TextLabel(this, 0, y + Scale(18), this.W, Scale(20), prefabDescription, UIColour.LightBlueText) { IsVisible = !string.IsNullOrEmpty(prefabDescription) };
            this.AddChild(this.prefabLabel);

            this.UpdateWidthAndHeight();
            this.ApplyLayout();
        }

        public override void ApplyLayout()
        {
            var y = Scale(this.resourceCosts.Any() ? 50 : 20);
            foreach (var label in this.descriptionLabels)
            {
                label.Y = y;
                label.W = this.W;
                label.H = Scale(20);
                y += LineHeight;
            }

            this.lockedReasonLabel.Y = y + Scale(18);
            this.lockedReasonLabel.W = this.W;
            this.lockedReasonLabel.H = Scale(20);

            this.prefabLabel.Y = y + Scale(18);
            this.prefabLabel.W = this.W;
            this.prefabLabel.H = Scale(20);

            if (this.resourceCosts.Any())
            {
                var tw = 67 + (this.energyAmountLabel.Text.Length * 7) + (this.timeAmountLabel.Text.Length * 7) + this.resourceLabels.Values.Sum(l => 48 + (l.Text.Length * 7));
                var tx = (UnScale(this.W, UIStatics.Scale) - tw) / 2;

                foreach (var c in this.resourceCosts)
                {
                    var label = this.resourceLabels[c.Key];
                    label.X = Scale(tx + 36);
                    label.Y = Scale(22);
                    tx += 48 + (label.Text.Length * 7);
                }

                this.energyAmountLabel.X = Scale(tx + 24);
                this.energyAmountLabel.Y = Scale(22);
                this.timeAmountLabel.X = Scale(tx + 60 + (this.energyAmountLabel.Text.Length * 7));
                this.timeAmountLabel.Y = Scale(22);
            }

            base.ApplyLayout();
        }

        protected override void UpdateWidthAndHeight()
        {
            var w = Scale(380);
            foreach (var label in this.descriptionLabels)
            {
                var w1 = Scale(20) + (LetterSpace * label.Text.Length);
                if (w1 > w) w = w1;
            }

            this.H = Scale(this.resourceCosts.Any() ? 60 : 30) + (this.descriptionLabels.Count() * LineHeight) + (this.lockedReasonLabel.IsVisible || this.prefabLabel.IsVisible ? LineHeight * 2 : 0);
            this.W = w;
        }

        public override void Update()
        {
            base.Update();
            if (this.Bottom > GameScreen.Instance.H - 1) this.Y = GameScreen.Instance.H - this.H - 1;
        }

        public void SetIsResourceAvailable(ItemType itemType, bool isAvailable)
        {
            if (this.resourceLabels.ContainsKey(itemType)) this.resourceLabels[itemType].Colour = isAvailable ? UIColour.DefaultText : UIColour.RedText;
        }

        private bool isEnergyAvailable = true;
        public bool IsEnergyAvailable
        {
            get { return this.isEnergyAvailable; }
            set
            {
                if (this.isEnergyAvailable != value)
                {
                    this.isEnergyAvailable = value;
                    this.energyAmountLabel.Colour = value ? UIColour.DefaultText : UIColour.RedText;
                }
            }
        }

        public bool IsLocked
        {
            get { return this.lockedReasonLabel.IsVisible; }
            set
            {
                if (this.lockedReasonLabel.IsVisible != value)
                {
                    this.lockedReasonLabel.IsVisible = value;
                    this.UpdateWidthAndHeight();
                    this.ApplyLayout();
                }
            }
        }

        public int PrefabCount
        {
            get { return this.prefabCount; }
            set
            {
                if (this.prefabCount != value)
                {
                    this.prefabCount = value;
                    this.prefabLabel.IsVisible = value > 0;
                    if (value > 0) this.prefabLabel.Text = $"{GetString(StringsForThingPanels.PrefabsAvailable)}: {prefabCount}";

                    this.UpdateWidthAndHeight();
                    this.ApplyLayout();
                }
            }
        }

        public override void LoadContent()
        {
            this.energyIcon.LoadContent();
            this.timeIcon.LoadContent();
            this.itemsIcon.LoadContent();
            base.LoadContent();
        }

        protected override void DrawBaseLayer()
        {
            if (this.attachedElement.IsMouseOver && this.attachedElement.IsVisible && this.IsEnabled)
            {
                var mx = -Scale(16);
                var my = -Scale(28);

                var w = mx + (this.W / 2);

                this.spriteBatch.Begin();
                this.spriteBatch.Draw(this.titleBackgroundTexture, new Rectangle(0, 0, this.W, LineHeight - 1), Color.White);
                this.spriteBatch.Draw(this.borderTexture, new Rectangle(0, 0, this.W, 1), Color.White);
                this.spriteBatch.Draw(this.borderTexture, new Rectangle(0, 0, 1, this.H), Color.White);
                this.spriteBatch.Draw(this.borderTexture, new Rectangle(0, this.H - 1, this.W, 1), Color.White);
                this.spriteBatch.Draw(this.borderTexture, new Rectangle(this.W - 1, 0, 1, this.H), Color.White);

                if (this.resourceCosts.Any())
                {
                    var tw = 67 + (this.energyAmountLabel.Text.Length * 7) + (this.timeAmountLabel.Text.Length * 7) + this.resourceLabels.Values.Sum(l => 48 + (l.Text.Length * 7));
                    var tx = (this.UnScale(this.W) - tw) / 2;

                    foreach (var c in this.resourceCosts)
                    {
                        this.itemsIcon.Draw(this.spriteBatch, Scale(tx), my + Scale(49), (int)c.Key - 1);
                        tx += 48 + (this.resourceLabels[c.Key].Text.Length * 7);
                    }

                    this.energyIcon.Draw(this.spriteBatch, Scale(tx), my + Scale(48));
                    this.timeIcon.Draw(this.spriteBatch, Scale(tx + 36 + (this.energyAmountLabel.Text.Length * 7)), my + Scale(49));
                }

                spriteBatch.End();
            }
        }

        protected static string GetString(StringsForThingPanels key)
        {
            return LanguageManager.Get<StringsForThingPanels>(key);
        }

        protected static string GetString(StringsForThingPanels key, object arg0)
        {
            return LanguageManager.Get<StringsForThingPanels>(key, arg0);
        }
    }
}
