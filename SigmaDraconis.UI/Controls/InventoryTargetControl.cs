namespace SigmaDraconis.UI
{
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using System;

    public sealed class InventoryTargetControl : UIElementBase
    {
        private readonly TickBoxTextButton tickButton;
        private readonly NumberPicker numberPicker;
        private readonly SimpleTooltip numberPickerTooltip;
        private readonly IconToggleButton actionOnCompleteButton;
        private readonly SimpleTooltip actionOnCompleteTooltip;
        private readonly string numberPickerTooltipTemplate;

        public event EventHandler<EventArgs> IsTargetEnabledChanged;
        public event EventHandler<EventArgs> TargetValueChanged;
        public event EventHandler<EventArgs> TargetActionOnCompleteChanged;

        public bool IsTargetEnabled
        {
            get => this.tickButton.IsTicked == true;
            set 
            {
                this.tickButton.IsTicked = value;
                this.numberPicker.IsVisible = value;
                if (this.actionOnCompleteButton != null) this.actionOnCompleteButton.IsVisible = value;
            }
        }

        public int TargetValue
        {
            get => this.numberPicker.Value;
            set
            {
                this.numberPicker.Value = value;
                this.numberPickerTooltip.SetTitle(string.Format(this.numberPickerTooltipTemplate, value));
            }
        }

        public bool IsStopOnComplete
        {
            get => this.actionOnCompleteButton?.IsOn == true;
            set 
            {
                if (this.actionOnCompleteButton == null) return;
                if (this.actionOnCompleteButton.IsOn != value) this.actionOnCompleteButton.Toggle();
                this.actionOnCompleteTooltip.SetTitle(GetString(this.actionOnCompleteButton.IsOn ? StringsForThingPanels.InventoryTargetStopOnComplete : StringsForThingPanels.InventoryTargetPauseOnComplete));
            }
        }

        public InventoryTargetControl(IUIElement parent, int x, int y, ItemType producedItemType, int defaultTarget, bool showActionOnCompleteButton = true) : base(parent, x, y, Scale(204), Scale(20))
        {
            this.tickButton = new TickBoxTextButton(this, 0, 0, Scale(60), Scale(18), GetString(StringsForThingPanels.Limit));
            this.AddChild(this.tickButton);

            UIHelper.AddSimpleTooltip(this.Parent, this.tickButton, StringsForThingPanels.LimitTooltip);

            this.numberPicker = new NumberPicker(this, Scale(62), 0, Scale(108), 1, 99, defaultTarget);
            this.AddChild(this.numberPicker);

            this.numberPickerTooltipTemplate = producedItemType != ItemType.None
                ? GetString(StringsForThingPanels.InventoryTargetTooltip).Replace("{1}", LanguageManager.Get<StringsForItemTypeLower>(producedItemType))
                : GetString(StringsForThingPanels.InventoryTargetTooltipMine);
            this.numberPickerTooltip = UIHelper.AddSimpleTooltip(this, this.numberPicker);

            if (showActionOnCompleteButton)
            {
                this.actionOnCompleteButton
                    = new IconToggleButton(this, Scale(172), 0, "Textures\\Icons\\StopWhenComplete", "Textures\\Icons\\PauseWhenComplete", 1f, true) { BackgroundColour = UIColour.ButtonBackground };
                this.AddChild(this.actionOnCompleteButton);
                this.actionOnCompleteButton.MouseLeftClick += this.OnInventoryTargetActionOnCompleteChanged;
                this.actionOnCompleteTooltip = UIHelper.AddSimpleTooltip(this, this.actionOnCompleteButton);
            }

            this.tickButton.MouseLeftClick += this.OnInventoryTargetTickButtonClick;
            this.numberPicker.ValueChanged += this.OnInventoryTargetValueChanged;
        }

        private void OnInventoryTargetTickButtonClick(object sender, MouseEventArgs e)
        {
            this.tickButton.IsTicked = !this.tickButton.IsTicked;
            this.IsTargetEnabledChanged.Invoke(this, e);
        }

        private void OnInventoryTargetValueChanged(object sender, EventArgs e)
        {
            this.TargetValueChanged.Invoke(this, e);
        }

        private void OnInventoryTargetActionOnCompleteChanged(object sender, MouseEventArgs e)
        {
            this.actionOnCompleteButton.Toggle();
            this.TargetActionOnCompleteChanged.Invoke(this, e);
        }

        private static string GetString(StringsForThingPanels key)
        {
            return LanguageManager.Get<StringsForThingPanels>(key);
        }
    }
}
