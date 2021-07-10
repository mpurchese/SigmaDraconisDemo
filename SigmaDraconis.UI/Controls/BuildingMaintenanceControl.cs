namespace SigmaDraconis.UI
{
    using System;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;

    public class BuildingMaintenanceControl : UIElementBase
    {
        protected readonly ProgressBar bar;
        protected readonly PriorityIconButton priorityButton;
        protected readonly SimpleTooltip priorityButtonTooltip;

        public WorkPriority RepairPriority { get { return this.priorityButton?.PriorityLevel ?? 0; } set { this.priorityButton.PriorityLevel = value; this.UpdatePriorityTooltip(); } }
        public event EventHandler<MouseEventArgs> PriorityChanged;

        public double MaintenanceLevel { get { return this.bar?.Fraction ?? 0.0; } set { this.bar.Fraction = value; } }

        public BuildingMaintenanceControl(IUIElement parent, int x, int y)
            : base(parent, x, y, Scale(248), Scale(20))
        {
            UIHelper.AddTextLabel(this, 0, 0, 248, UIColour.DefaultText, StringsForThingPanels.Maintenance);

            this.bar = new ProgressBar(this, Scale(20), Scale(16), Scale(200), Scale(4)) { BarColour = UIColour.BuildingMaintenanceBar };
            this.AddChild(this.bar);

            this.priorityButton = new PriorityIconButton(this, Scale(228), 0, "Textures\\Icons\\RepairPriority");
            this.AddChild(this.priorityButton);
            this.priorityButton.MouseLeftClick += this.OnPriorityButtonLeftClick;
            this.priorityButton.MouseRightClick += this.OnPriorityButtonRightClick;

            this.priorityButtonTooltip = UIHelper.AddSimpleTooltip(this.Parent, this.priorityButton);
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

        protected void UpdatePriorityTooltip()
        {
            if (this.priorityButtonTooltip == null) return;
            this.priorityButtonTooltip.SetTitle(GetString((StringsForThingPanels)((int)StringsForThingPanels.MaintenancePriority0 + this.RepairPriority)));
        }

        protected override void HandleLanguageChange()
        {
            this.UpdatePriorityTooltip();
            base.HandleLanguageChange();
        }

        protected static string GetString(StringsForThingPanels key)
        {
            return LanguageManager.Get<StringsForThingPanels>(key);
        }
    }
}
