namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using Draconis.Shared;
    using Draconis.UI;
    using Config;
    using Language;
    using Managers;
    using Shared;
    using World;
    using World.Projects;

    public class FarmPanel : PanelLeft
    {
        private readonly HorizontalStack cropButtonStack1;
        private readonly HorizontalStack cropButtonStack2;
        private readonly HorizontalStack optionButtonStack;
        private readonly Dictionary<CropButton, int> cropButtons = new Dictionary<CropButton, int>();
        private CropButton kekkeButton;
        private CropTooltip kekkeTooltip;
        private readonly TickBoxTextButton replaceExistingButton;
        private readonly TextButton setDefaultButton;
        private readonly TextLabelAutoScaling instructionLabel1;
        private readonly TextLabelAutoScaling instructionLabel2;
        private int? selectedCropId;
        private bool canPlantKekke = true;

        public FarmPanel(IUIElement parent, int y)
            : base(parent, y, Scale(320), Scale(200), GetString(StringsForFarmPanel.Title))
        {
            this.cropButtonStack1 = new HorizontalStack(this, 0, Scale(22), Scale(320), Scale(48), TextAlignment.TopCentre) { Spacing = 4, PaddingTop = 0 };
            this.cropButtonStack2 = new HorizontalStack(this, 0, Scale(74), Scale(320), Scale(48), TextAlignment.TopCentre) { Spacing = 4, PaddingTop = 0 };
            this.AddChild(this.cropButtonStack1);
            this.AddChild(this.cropButtonStack2);

            this.AddCropButton(this.cropButtonStack1, 0);
            this.AddCropButton(this.cropButtonStack1, 1);
            this.AddCropButton(this.cropButtonStack1, 2);
            this.AddCropButton(this.cropButtonStack1, 3);
            this.AddCropButton(this.cropButtonStack2, 4);
            this.AddCropButton(this.cropButtonStack2, 5);
            this.AddCropButton(this.cropButtonStack2, 6);

            this.optionButtonStack = new HorizontalStack(this, 0, Scale(130), Scale(320), Scale(20), TextAlignment.TopCentre) { Spacing = 8, PaddingTop = 0 };
            this.AddChild(this.optionButtonStack);

            var removeExistingStr = GetString(StringsForFarmPanel.RemoveExisting);
            this.replaceExistingButton = new TickBoxTextButton(this.optionButtonStack, 0, 0, Scale(28 + (removeExistingStr.Length * 7)), Scale(20), removeExistingStr);
            this.optionButtonStack.AddChild(this.replaceExistingButton);
            this.replaceExistingButton.MouseLeftClick += this.OnReplaceExistingButtonClick;
            var tooltip = new SimpleTooltip(this, this.replaceExistingButton, "", GetString(StringsForFarmPanel.RemoveExistingTooltip));
            TooltipParent.Instance.AddChild(tooltip);

            var setDefaultStr = GetString(StringsForFarmPanel.SetDefault);
            this.setDefaultButton = new TextButton(this.optionButtonStack, 0, 0, Scale(12 + (setDefaultStr.Length * 7)), Scale(20), setDefaultStr) { IsEnabled = false };
            this.optionButtonStack.AddChild(this.setDefaultButton);
            this.setDefaultButton.MouseLeftClick += this.OnSetDefaultButtonClick;
            tooltip = new SimpleTooltip(this, this.setDefaultButton, "", GetString(StringsForFarmPanel.SetDefaultTooltip));
            TooltipParent.Instance.AddChild(tooltip);

            var instructions = GetString(StringsForFarmPanel.Instruction1).Split('|');
            this.instructionLabel1 = new TextLabelAutoScaling(this, 0, 154, 320, 18, instructions[0], UIColour.DefaultText);
            this.AddChild(this.instructionLabel1);

            this.instructionLabel2 = new TextLabelAutoScaling(this, 0, 172, 320, 18, instructions.Length > 0 ? instructions[1] : "", UIColour.DefaultText);
            this.AddChild(this.instructionLabel2);
        }

        protected override void HandleLanguageChange()
        {
            this.Title = GetString(StringsForFarmPanel.Title);

            this.replaceExistingButton.Text = GetString(StringsForFarmPanel.RemoveExisting);
            this.replaceExistingButton.W = Scale(28 + (this.replaceExistingButton.Text.Length * 7));

            this.setDefaultButton.Text = GetString(StringsForFarmPanel.SetDefault);
            this.setDefaultButton.W = Scale(12 + (this.setDefaultButton.Text.Length * 7));

            this.optionButtonStack.LayoutInvalidated = true;

            this.SetInstructionText();

            base.HandleLanguageChange();
        }

        private void OnReplaceExistingButtonClick(object sender, MouseEventArgs e)
        {
            this.replaceExistingButton.IsTicked = !this.replaceExistingButton.IsTicked;
            PlayerActivityFarm.IsReplaceExisting = this.replaceExistingButton.IsTicked;
            this.SetInstructionText();
        }

        private void OnSetDefaultButtonClick(object sender, MouseEventArgs e)
        {
            this.setDefaultButton.IsEnabled = false;
            if (this.selectedCropId.HasValue) World.DefaultCropId = this.selectedCropId.Value;
            foreach (var kv in this.cropButtons) kv.Key.IsOverlayVisible = kv.Value == World.DefaultCropId;
        }

        public override void LoadContent()
        {
            PlayerWorldInteractionManager.CurrentActivityChanged += this.OnCurrentActivityChanged;
            base.LoadContent();
        }

        public override void Show()
        {
            foreach (var kv in this.cropButtons) kv.Key.IsOverlayVisible = kv.Value == World.DefaultCropId;
            base.Show();
        }

        public override void Update()
        {
            if (!this.IsVisible) return;

            var kekProject = ProjectManager.GetDefinition(11);
            var newCanPlantKekke = kekProject?.IsDone == true;
            this.kekkeButton.IsLocked = !newCanPlantKekke;
            if (newCanPlantKekke != this.canPlantKekke)
            {
                this.canPlantKekke = newCanPlantKekke;
                var lockedDescription = "";
                var definition = CropDefinitionManager.GetDefinition(6);
                if (!this.canPlantKekke && kekProject != null)
                {
                    // Kekke requires project
                    var labType = LanguageManager.GetName(ThingType.Biolab);
                    lockedDescription = LanguageManager.Get<StringsForConstructPanel>(StringsForConstructPanel.RequiresProject, kekProject.DisplayName, labType);
                }

                this.kekkeTooltip.SetDefinition(definition, lockedDescription);
            }

            base.Update();
        }

        private void AddCropButton(IUIElement parent, int cropId)
        {
            var button = new CropButton(parent, 0, 0, "Textures\\Icons\\Crop" + cropId) { IsOverlayVisible = cropId == World.DefaultCropId };
            this.cropButtons.Add(button, cropId);
            button.MouseLeftClick += this.OnCropButtonClick;
            parent.AddChild(button);

            if (cropId > 0)
            {
                var definition = CropDefinitionManager.GetDefinition(cropId);
                var tooltip = new CropTooltip(TooltipParent.Instance, button, definition);
                TooltipParent.Instance.AddChild(tooltip);
                if (cropId == 6)
                {
                    this.kekkeButton = button;
                    this.kekkeTooltip = tooltip;
                }
            }
            else
            {
                var tooltip = new SimpleTooltip(TooltipParent.Instance, button, GetString(StringsForFarmPanel.NoCrop));
                TooltipParent.Instance.AddChild(tooltip);
            }
        }

        private void OnCropButtonClick(object sender, MouseEventArgs e)
        {
            var button = sender as CropButton;
            if (button.IsSelected)
            {
                PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
                button.IsSelected = false;
                this.selectedCropId = null;
                var instructions = GetString(StringsForFarmPanel.Instruction1).Split('|');
                this.instructionLabel1.Text = instructions[0];
                this.instructionLabel2.Text = instructions.Length > 0 ? instructions[1] : "";
                this.setDefaultButton.IsEnabled = false;
            }
            else if (button.IsEnabled)
            {
                PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.Farm, PlayerActivitySubType.None);
                this.selectedCropId = this.cropButtons[button];
                PlayerActivityFarm.CropType = this.selectedCropId.Value;
                foreach (var b in this.cropButtons.Keys) b.IsSelected = b == button;
                this.SetInstructionText();
                this.setDefaultButton.IsEnabled = this.selectedCropId.Value != World.DefaultCropId;
            }
        }

        private void SetInstructionText()
        {
            if (this.selectedCropId > 0)
            {
                var cropName = CropDefinitionManager.GetDefinition(this.selectedCropId.Value).DisplayName;
                var instructions = GetString(this.replaceExistingButton.IsTicked ? StringsForFarmPanel.Instruction4 : StringsForFarmPanel.Instruction3, cropName).Split('|');
                this.instructionLabel1.Text = instructions[0];
                this.instructionLabel2.Text = instructions.Length > 0 ? instructions[1] : "";
            }
            else if (this.selectedCropId == 0)
            {
                var instructions = GetString(this.replaceExistingButton.IsTicked ? StringsForFarmPanel.Instruction6 : StringsForFarmPanel.Instruction5).Split('|');
                this.instructionLabel1.Text = instructions[0];
                this.instructionLabel2.Text = instructions.Length > 0 ? instructions[1] : "";
            }
            else
            {
                var instructions = GetString(StringsForFarmPanel.Instruction1).Split('|');
                this.instructionLabel1.Text = instructions[0];
                this.instructionLabel2.Text = instructions.Length > 0 ? instructions[1] : "";
            }
        }

        private void OnCurrentActivityChanged(object sender, EventArgs e)
        {
            if (PlayerWorldInteractionManager.CurrentActivity != PlayerActivityType.Farm)
            {
                foreach (var button in this.cropButtons) button.Key.IsSelected = false;
            }
        }

        private static string GetString(StringsForFarmPanel value)
        {
            return LanguageManager.Get<StringsForFarmPanel>(value);
        }

        private static string GetString(StringsForFarmPanel value, string str1)
        {
            return LanguageManager.Get<StringsForFarmPanel>(value, str1);
        }
    }
}
