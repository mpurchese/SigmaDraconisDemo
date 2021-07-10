namespace SigmaDraconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;

    public class BuildingStatusControl : UIElementBase
    {
        protected readonly BuildingPanelStatusLabel textLabel;
        protected readonly ProgressBar bar;
        protected readonly PriorityIconButton priorityButton;
        protected readonly SimpleTooltip priorityButtonTooltip;
        protected readonly AutoRestartIconButton autoRestartButton;
        protected readonly SimpleTooltip autoRestartButtonTooltip;
        protected readonly SimpleTooltip timeRemainingTooltip;

        public WorkPriority WorkPriority
        { 
            get { return this.priorityButton?.PriorityLevel ?? WorkPriority.Disabled; }
            set { this.priorityButton.PriorityLevel = value; this.UpdatePriorityTooltip(); }
        }

        public bool IsAutoRestartEnabled
        {
            get { return this.autoRestartButton?.IsOn ?? false; }
            set { if (this.autoRestartButton != null) { this.autoRestartButton.IsOn = value; this.UpdateAutoRestartTooltip(); } }
        }

        public double ProgressFraction { get { return this.bar?.Fraction ?? 0; } set { this.bar.Fraction = value; } }

        public event EventHandler<MouseEventArgs> AutoRestartChanged;
        public event EventHandler<MouseEventArgs> PriorityChanged;

        public double MaintenanceLevel { get { return this.bar?.Fraction ?? 0.0; } set { this.bar.Fraction = value; } }

        public BuildingStatusControl(IUIElement parent, int x, int y, int width, int height, bool showPriorityButton = false, bool showAutoRestartButton = false)
            : base(parent, x, y, width, height)
        {
            this.textLabel = new BuildingPanelStatusLabel(this, 0, 0, width);
            this.AddChild(this.textLabel);

            this.bar = new ProgressBar(this, Scale(20), height - Scale(4), width - Scale(48), Scale(4)) { BarColour = UIColour.BuildingWorkBar };
            this.AddChild(this.bar);

            var tooltipAttachElement = new EmptyElement(this, Scale(20), 0, width - Scale(48), height);
            this.AddChild(tooltipAttachElement);
            this.timeRemainingTooltip = UIHelper.AddSimpleTooltip(this.Parent, tooltipAttachElement, "");
            this.timeRemainingTooltip.IsEnabled = false;

            if (showPriorityButton)
            {
                this.priorityButton = new PriorityIconButton(this, width - Scale(20), 0, "Textures\\Icons\\WorkPriority", WorkPriority.Low);
                this.AddChild(this.priorityButton);
                this.priorityButton.MouseLeftClick += this.OnPriorityButtonLeftClick;
                this.priorityButton.MouseRightClick += this.OnPriorityButtonRightClick;
                this.priorityButtonTooltip = UIHelper.AddSimpleTooltip(this.Parent, this.priorityButton, LanguageManager.Get<WorkPriority>(this.WorkPriority));
            }
            else if (showAutoRestartButton)
            {
                this.autoRestartButton = new AutoRestartIconButton(this, width - Scale(20), 0);
                this.AddChild(this.autoRestartButton);
                this.autoRestartButton.MouseLeftClick += this.OnAutoRestartButtonClick;
                this.autoRestartButton.MouseRightClick += this.OnAutoRestartButtonClick;
                this.autoRestartButtonTooltip = UIHelper.AddSimpleTooltip(this.Parent, this.autoRestartButton, GetAutoRestartTooltipTitle());
            }
        }

        public void SetTimeRemaining(int frames)
        {
            if (frames > 0)
            {
                this.timeRemainingTooltip.IsEnabled = true;
                var hours = frames / 3600;
                var minutes = (frames - (hours * 3600)) / 60;
                this.timeRemainingTooltip.SetTitle(LanguageManager.Get<StringsForThingPanels>(StringsForThingPanels.TimeRemaining, hours, minutes));
            }
            else this.timeRemainingTooltip.IsEnabled = false;
        }

        protected void OnAutoRestartButtonClick(object sender, MouseEventArgs e)
        {
            this.autoRestartButton.IsOn = !this.autoRestartButton.IsOn;
            this.UpdateAutoRestartTooltip();
            this.AutoRestartChanged?.Invoke(this, e);
        }

        protected void OnPriorityButtonLeftClick(object sender, MouseEventArgs e)
        {
            this.priorityButton.IncreasePriority();
            this.UpdatePriorityTooltip();
            this.PriorityChanged?.Invoke(this, e);
        }

        protected void OnPriorityButtonRightClick(object sender, MouseEventArgs e)
        {
            this.priorityButton.DecreasePriority();
            this.UpdatePriorityTooltip();
            this.PriorityChanged?.Invoke(this, e);
        }

        public void SetStatus(BuildingDisplayStatus newStatus, Color colour)
        {
            this.textLabel.SetStatus(newStatus, colour);
        }

        public void SetStatus(BuildingDisplayStatus newStatus, Color colour, Color barColour)
        {
            this.bar.BarColour = barColour;
            this.textLabel.SetStatus(newStatus, colour);
        }

        protected void UpdatePriorityTooltip()
        {
            if (this.priorityButtonTooltip == null) return;
            this.priorityButtonTooltip.SetTitle(LanguageManager.Get<WorkPriority>(this.WorkPriority));
        }

        protected void UpdateAutoRestartTooltip()
        {
            if (this.autoRestartButtonTooltip == null) return;
            this.autoRestartButtonTooltip.SetTitle(GetAutoRestartTooltipTitle());
        }

        protected override void HandleLanguageChange()
        {
            this.UpdatePriorityTooltip();
            this.UpdateAutoRestartTooltip();
            base.HandleLanguageChange();
        }

        private string GetAutoRestartTooltipTitle()
        {
            return LanguageManager.Get<StringsForThingPanels>(this.autoRestartButton.IsOn ? StringsForThingPanels.AutoRestartOn : StringsForThingPanels.AutoRestartOff);
        }
    }
}
