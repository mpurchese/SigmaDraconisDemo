namespace SigmaDraconis.UI
{
    using System;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;

    public class BuildingConstructionControl : UIElementBase
    {
        private readonly TextLabel textLabel;
        private readonly ProgressBar bar;
        private readonly PriorityIconButton priorityButton;
        private readonly SimpleTooltip priorityButtonTooltip;

        private bool isDeconstructing;
        private StringsForThingPanels constructionStringId;

        public WorkPriority Priority { get { return this.priorityButton?.PriorityLevel ?? 0; } set { this.priorityButton.PriorityLevel = value; this.UpdatePriorityTooltip(); } }

        public StringsForThingPanels ConstructionStringId
        {
            get 
            { 
                return this.constructionStringId; 
            }
            set
            {
                if (this.constructionStringId == value) return;
                this.textLabel.Text = GetString(this.isDeconstructing ? StringsForThingPanels.DeconstructionProgress : this.constructionStringId);
                this.constructionStringId = value;
            } 
        }

        public double Progress { get { return this.bar?.Fraction ?? 0.0; } set { this.bar.Fraction = value; } }

        public bool IsDeconstructing
        {
            get { return this.isDeconstructing; }
            set
            {
                if (this.isDeconstructing == value) return;

                this.isDeconstructing = value;
                this.priorityButton.IsVisible = !value;
                this.textLabel.Text = GetString(value ? StringsForThingPanels.DeconstructionProgress : this.constructionStringId);
                this.bar.BarColour = value ? UIColour.BuildingDeconstructionBar : UIColour.BuildingConstructionBar;
            }
        }

        public event EventHandler<MouseEventArgs> PriorityChanged;

        public BuildingConstructionControl(IUIElement parent, int x, int y, int width, int height)
            : base(parent, x, y, width, height)
        {
            this.constructionStringId = StringsForThingPanels.ConstructionProgress;

            this.textLabel = new TextLabel(this, 0, 0, width, Scale(14), GetString(StringsForThingPanels.ConstructionProgress), UIColour.DefaultText);
            this.AddChild(this.textLabel);

            this.bar = new ProgressBar(this, Scale(22), height - Scale(4), width - Scale(46), Scale(4)) { BarColour = UIColour.BuildingConstructionBar };
            this.AddChild(this.bar);

            this.priorityButton = new PriorityIconButton(this, width - Scale(20), 0, "Textures\\Icons\\ConstructionPriority");
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
            this.priorityButtonTooltip.SetTitle(GetString((StringsForThingPanels)((int)StringsForThingPanels.ConstructionPriority0 + this.Priority)));
        }

        protected override void HandleLanguageChange()
        {
            this.textLabel.Text = GetString(this.IsDeconstructing ? StringsForThingPanels.DeconstructionProgress : this.constructionStringId);
            this.UpdatePriorityTooltip();
            base.HandleLanguageChange();
        }

        protected static string GetString(StringsForThingPanels key)
        {
            return LanguageManager.Get<StringsForThingPanels>(key);
        }
    }
}
