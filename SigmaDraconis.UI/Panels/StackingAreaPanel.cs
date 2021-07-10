namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Draconis.UI;
    using AI;
    using Language;
    using Shared;
    using World;
    using WorldInterfaces;

    public class StackingAreaPanel : PanelLeft, IThingPanel
    {
        private readonly HorizontalStack buttonStack1;
        private readonly Dictionary<TickBoxIconButton, ItemType> itemTypeButtons = new Dictionary<TickBoxIconButton, ItemType>();

        private readonly LeftRightPicker modePicker;
        private readonly SimpleTooltip modePickerTooltip;
        private readonly NumberPicker inventoryTargetNumberPicker;
        private readonly SimpleTooltip inventoryTargetNumberPickerTooltip;
        private readonly PriorityIconButton priorityButton;
        private readonly SimpleTooltip priorityButtonTooltip;
        private string stackingAreaStr;

        private readonly TextButton applyToAllButton;
        private readonly SimpleTooltip applyToAllButtonTooltip;

        private readonly TextLabel appliedToAllLabel;
        private readonly TextLabel currentStackLabel;

        protected IStackingArea stackingArea;
        public IThing Thing
        {
            get { return this.stackingArea; }
            set
            {
                if (this.stackingArea != value)
                {
                    this.stackingArea = value as IStackingArea;
                    this.isAppliedToAll = false;
                    this.UpdatePriorityTooltip();
                    UpdateTargetNumberPicker();
                    this.modePicker.SelectedIndex = (int)this.stackingArea.Mode;
                    this.Update();
                }
            }
        }

        private bool isAppliedToAll;

        public StackingAreaPanel(IUIElement parent, int y)
            : base(parent, y, Scale(320), Scale(126), "")
        {
            this.buttonStack1 = new HorizontalStack(this, 0, Scale(18), Scale(320), Scale(22), TextAlignment.MiddleRight) { Spacing = Scale(2) };
            this.AddChild(this.buttonStack1);

            this.AddItemTypeButton(ItemType.Metal);
            this.AddItemTypeButton(ItemType.Stone);
            this.AddItemTypeButton(ItemType.IronOre);
            this.AddItemTypeButton(ItemType.Coal);
            this.AddItemTypeButton(ItemType.Biomass);
            this.AddItemTypeButton(ItemType.Compost);

            this.modePicker = this.AddChild(new LeftRightEnumPicker<StackingAreaMode>(this, 6, 44, 176, 0, true));
            this.modePicker.SelectedIndexChanged += this.OnModeChanged;
            this.modePickerTooltip = UIHelper.AddSimpleTooltip(this, this.modePicker, "", GetString(StringsForThingPanels.StackingAreaModeTooltip), TextAlignment.TopLeft);

            this.inventoryTargetNumberPicker = this.AddChild(new NumberPicker(this, Scale(184), Scale(44), Scale(110), 0, 99, 10));
            this.inventoryTargetNumberPicker.ValueChanged += this.OnTargetNumberChanged;

            this.inventoryTargetNumberPickerTooltip = UIHelper.AddSimpleTooltip(this, this.inventoryTargetNumberPicker, "", "");

            this.priorityButton = this.AddChild(new PriorityIconButton(this, Scale(298), Scale(44), "Textures\\Icons\\WorkPrioritySmall"));
            this.priorityButton.MouseLeftClick += this.OnPriorityButtonLeftClick;
            this.priorityButton.MouseRightClick += this.OnPriorityButtonRightClick;

            this.priorityButtonTooltip = UIHelper.AddSimpleTooltip(this, this.priorityButton);
            
            this.applyToAllButton = UIHelper.AddTextButton(this, 66, StringsForButtons.ApplyToAll);
            this.applyToAllButtonTooltip = UIHelper.AddSimpleTooltip(this, this.applyToAllButton, "", "");
            this.applyToAllButton.MouseLeftClick += this.OnApplyToAllButtonClick;

            this.appliedToAllLabel = this.AddChild(new TextLabel(this, 0, Scale(86), Scale(320), Scale(20), "", UIColour.GreenText) { IsVisible = false });

            this.currentStackLabel = this.AddChild(new TextLabel(this, 0, Scale(108), Scale(320), Scale(20), "", UIColour.DefaultText));

            this.stackingAreaStr = LanguageManager.GetName(ThingType.StackingArea).ToUpperInvariant();

            var deleteButton = UIHelper.AddIconButton(this, 300, 106, "Textures\\Icons\\Cross", this.OnDeleteClick);
            UIHelper.AddSimpleTooltip(this, deleteButton, StringsForThingPanels.RemoveStackingArea);
        }

        public override void Update()
        {
            if (this.stackingArea != null)
            {
                var itemTypeName = LanguageManager.Get<ItemType>(this.stackingArea.ItemType);
                var itemTypeNameLower = LanguageManager.Get<StringsForItemTypeLower>(this.stackingArea.ItemType);
                this.titleLabel.Text = $"{this.stackingAreaStr} ({itemTypeName})";
                
                foreach (var kv in this.itemTypeButtons) kv.Key.IsTicked = this.stackingArea.ItemType == kv.Value;

                if (this.stackingArea.Mode != StackingAreaMode.OverflowOnly && this.stackingArea.Mode != StackingAreaMode.RemoveStack)
                {
                    this.inventoryTargetNumberPicker.IsVisible = true;
                    var template = this.stackingArea.Mode == StackingAreaMode.TargetStackSize ? GetString(StringsForThingPanels.SiloLevelTargetTooltip2) : GetString(StringsForThingPanels.SiloLevelTargetTooltip1);
                    this.inventoryTargetNumberPickerTooltip.SetText(string.Format(template, itemTypeNameLower));
                }
                else this.inventoryTargetNumberPicker.IsVisible = false;

                this.applyToAllButton.IsEnabled = !this.isAppliedToAll && World.GetThings<IStackingArea>(ThingType.StackingArea).Any(s => s.ItemType == this.stackingArea.ItemType && s != this.stackingArea);
                if (this.applyToAllButton.IsEnabled) this.applyToAllButtonTooltip.SetText(GetString(StringsForThingPanels.ApplyStackingTargetToAll, itemTypeNameLower));
                this.applyToAllButtonTooltip.IsEnabled = this.applyToAllButton.IsEnabled;
                this.appliedToAllLabel.IsVisible = this.isAppliedToAll;
                if (this.appliedToAllLabel.IsVisible) this.appliedToAllLabel.Text = GetString(StringsForThingPanels.TargetAppliedToAllStackingAreas, itemTypeNameLower);

                if (this.stackingArea.WorkPriority != this.priorityButton.PriorityLevel)
                {
                    this.priorityButton.PriorityLevel = this.stackingArea.WorkPriority;
                    this.UpdatePriorityTooltip();
                }

                var stack = this.stackingArea.MainTile.ThingsPrimary.OfType<IResourceStack>().FirstOrDefault();
                if (stack != null)
                {
                    if (stack.ItemType != this.stackingArea.ItemType) itemTypeName = LanguageManager.Get<ItemType>(stack.ItemType);
                    this.currentStackLabel.Text = LanguageManager.Get<StringsForThingPanels>(StringsForThingPanels.CurrentStack, stack.ItemCount, itemTypeName);
                }
                else this.currentStackLabel.Text = GetString(StringsForThingPanels.CurrentStackNone);
            }

            base.Update();
        }

        private void AddItemTypeButton(ItemType itemType)
        {
            var button = new TickBoxIconButton(this.buttonStack1, 0, 0, Scale(50), Scale(22), new Icon("Textures\\Icons\\Items", 13), (int)itemType - 1, true);
            button.MouseLeftClick += this.OnItemTypeButtonClick;
            this.buttonStack1.AddChild(button);
            this.itemTypeButtons.Add(button, itemType);
        }

        private void OnPriorityButtonLeftClick(object sender, MouseEventArgs e)
        {
            this.priorityButton.IncreasePriority();
            this.UpdatePriorityTooltip();
            if (this.stackingArea.WorkPriority != this.priorityButton.PriorityLevel)
            {
                this.stackingArea.WorkPriority = this.priorityButton.PriorityLevel;
                this.stackingArea.UpdateStack();
            }
        }

        private void OnPriorityButtonRightClick(object sender, MouseEventArgs e)
        {
            this.priorityButton.DecreasePriority();
            this.UpdatePriorityTooltip();
            if (this.stackingArea.WorkPriority != this.priorityButton.PriorityLevel)
            {
                this.stackingArea.WorkPriority = this.priorityButton.PriorityLevel;
                this.stackingArea.UpdateStack();
            }
        }

        private void UpdatePriorityTooltip()
        {
            if (this.priorityButtonTooltip == null || this.stackingArea == null) return;
            this.priorityButtonTooltip.SetTitle(LanguageManager.Get<WorkPriority>(this.priorityButton.PriorityLevel));
        }

        private void UpdateTargetNumberPicker()
        {
            if (this.priorityButtonTooltip == null || this.stackingArea == null) return;

            this.inventoryTargetNumberPicker.Max = this.stackingArea.Mode == StackingAreaMode.TargetSiloLevel ? 99 : Constants.ResourceStackMaxSizes[this.stackingArea.ItemType];

            if (this.stackingArea.Mode == StackingAreaMode.TargetSiloLevel) this.inventoryTargetNumberPicker.Value = ResourceStackingController.GetTarget(this.stackingArea.ItemType);
            else if (this.stackingArea.Mode == StackingAreaMode.TargetStackSize) this.inventoryTargetNumberPicker.Value = this.stackingArea.TargetStackSize;
        }

        private void OnModeChanged(object sender, EventArgs e)
        {
            if (this.stackingArea == null) return;

            this.stackingArea.Mode = (StackingAreaMode)this.modePicker.SelectedIndex;
            if (this.stackingArea.Mode == StackingAreaMode.TargetStackSize) this.stackingArea.TargetStackSize = this.inventoryTargetNumberPicker.Value;

            this.UpdateTargetNumberPicker();
            this.stackingArea.UpdateStack();
            this.isAppliedToAll = false;
            this.Update();
        }

        private void OnTargetNumberChanged(object sender, EventArgs e)
        {
            if (this.stackingArea == null) return;

            if (this.stackingArea.Mode == StackingAreaMode.TargetStackSize) this.stackingArea.TargetStackSize = this.inventoryTargetNumberPicker.Value;
            else if (this.stackingArea.Mode == StackingAreaMode.TargetSiloLevel) ResourceStackingController.SetTarget(this.stackingArea.ItemType, this.inventoryTargetNumberPicker.Value);

            this.stackingArea.UpdateStack();
            this.isAppliedToAll = false;
            this.Update();
        }

        private void OnApplyToAllButtonClick(object sender, MouseEventArgs e)
        {
            if (this.stackingArea == null) return;

            foreach (var sa in World.GetThings<IStackingArea>(ThingType.StackingArea).Where(s => s.ItemType == this.stackingArea.ItemType && s != this.stackingArea).ToList())
            {
                sa.Mode = this.stackingArea.Mode;
                sa.WorkPriority = this.stackingArea.WorkPriority;
                if (this.stackingArea.Mode == StackingAreaMode.TargetStackSize) sa.TargetStackSize = this.stackingArea.TargetStackSize;
                sa.UpdateStack();
            }

            this.isAppliedToAll = true;
            this.Update();
        }

        private void OnItemTypeButtonClick(object sender, MouseEventArgs e)
        {
            if (this.stackingArea != null && sender is TickBoxIconButton b)
            {
                this.stackingArea.ItemType = this.itemTypeButtons[b];
                if (this.stackingArea.Mode == StackingAreaMode.TargetStackSize)
                {
                    this.stackingArea.TargetStackSize = Math.Min(this.stackingArea.TargetStackSize, Constants.ResourceStackMaxSizes[this.stackingArea.ItemType]);
                    this.UpdateTargetNumberPicker();
                }
                else if (this.stackingArea.Mode == StackingAreaMode.TargetSiloLevel) UpdateTargetNumberPicker();

                this.stackingArea.UpdateStack();
                EventManager.EnqueueWorldPropertyChangeEvent(this.Thing.Id, nameof(IStackingArea.ItemType));
                this.isAppliedToAll = false;
                this.Update();
            }
        }

        private void OnDeleteClick(object sender, MouseEventArgs e)
        {
            if (this.stackingArea == null) return;

            this.stackingArea.Mode = StackingAreaMode.RemoveStack;
            this.stackingArea.UpdateStack();
            World.RemoveThing(this.stackingArea);
            this.Hide();
        }

        protected override void HandleLanguageChange()
        {
            this.modePickerTooltip.SetText(GetString(StringsForThingPanels.StackingAreaModeTooltip));
            this.stackingAreaStr = LanguageManager.GetName(ThingType.StackingArea).ToUpperInvariant();

            base.HandleLanguageChange();
        }

        private static string GetString(StringsForThingPanels key)
        {
            return LanguageManager.Get<StringsForThingPanels>(key);
        }

        private static string GetString(StringsForThingPanels key, object arg0)
        {
            return LanguageManager.Get<StringsForThingPanels>(key, arg0);
        }
    }
}
