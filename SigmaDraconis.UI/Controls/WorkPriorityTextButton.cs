namespace SigmaDraconis.UI
{
    using System;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Microsoft.Xna.Framework;
    using Shared;

    public class WorkPriorityTextButton : TextButton
    {
        protected readonly PriorityIconButton priorityButton;
        protected readonly SimpleTooltip priorityTooltip;

        public WorkPriority PriorityLevel
        {
            get { return this.priorityButton.PriorityLevel; }
            set
            {
                if (this.priorityButton.PriorityLevel != value)
                {
                    this.priorityButton.PriorityLevel = value;
                    this.priorityTooltip.SetTitle(LanguageManager.Get<WorkPriority>(this.priorityButton.PriorityLevel));
                }
            }
        }

        public event EventHandler<EventArgs> PriorityChanged;

        public WorkPriorityTextButton(IUIElement parent, int x, int y, int width, string text)
            : base( parent, x, y, width, Scale(20), text)
        {
            this.textLabel.TextAlign = TextAlignment.MiddleCentre;
            this.textLabel.X = Scale(20);
            this.textLabel.W = this.W - Scale(20);

            this.priorityButton = new PriorityIconButton(this, 0, 0, "Textures\\Icons\\WorkPriority");
            this.AddChild(this.priorityButton);
            this.priorityButton.MouseLeftClick += this.OnPriorityButtonLeftClick;
            this.priorityButton.MouseRightClick += this.OnPriorityButtonRightClick;

            this.priorityTooltip = new SimpleTooltip(TooltipParent.Instance, this);
            TooltipParent.Instance.AddChild(this.priorityTooltip);
            this.priorityTooltip.SetTitle(LanguageManager.Get<WorkPriority>(this.priorityButton.PriorityLevel));

            this.BackgroundColour = new Color(0, 0, 0, 100);
        }

        public override void Update()
        {
            base.Update();
            this.priorityButton.IsMouseOver = this.IsMouseOver && this.IsEnabled;
            this.priorityTooltip.IsEnabled = this.isEnabled;
        }

        protected override void HandleLanguageChange()
        {
            this.priorityTooltip.SetTitle(LanguageManager.Get<WorkPriority>(this.priorityButton.PriorityLevel));
            base.HandleLanguageChange();
        }

        protected void OnPriorityButtonLeftClick(object sender, MouseEventArgs e)
        {
            if (!this.IsEnabled) return;
            this.priorityButton.IncreasePriority();
            this.priorityTooltip.SetTitle(LanguageManager.Get<WorkPriority>(this.priorityButton.PriorityLevel));
            this.PriorityChanged?.Invoke(this, new EventArgs());
        }

        protected void OnPriorityButtonRightClick(object sender, MouseEventArgs e)
        {
            if (!this.IsEnabled) return;
            this.priorityButton.DecreasePriority();
            this.priorityTooltip.SetTitle(LanguageManager.Get<WorkPriority>(this.priorityButton.PriorityLevel));
            this.PriorityChanged?.Invoke(this, new EventArgs());
        }

        protected override void OnMouseLeftClick(MouseEventArgs e)
        {
            if (!this.IsEnabled) return;
            this.OnPriorityButtonLeftClick(this, e);
        }

        protected override void OnMouseRightClick(MouseEventArgs e)
        {
            if (!this.IsEnabled) return;
            this.OnPriorityButtonRightClick(this, e);
        }
    }
}
