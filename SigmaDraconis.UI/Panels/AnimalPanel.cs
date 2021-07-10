namespace SigmaDraconis.UI
{
    using System;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using World;
    using WorldInterfaces;

    public class AnimalPanel : PanelLeft, IThingPanelWithFollowButton
    {
        private readonly CameraTrackingIconButton followButton;

        protected IAnimal animal;
        public IThing Thing
        {
            get { return this.animal; }
            set { this.animal = value as IAnimal; this.UpdateTitle(); }
        }

        public event EventHandler<EventArgs> FollowButtonClick;

        public AnimalPanel(IUIElement parent, int y)
            : base(parent, y, Scale(320), Scale(100), "")
        {
            this.followButton = new CameraTrackingIconButton(this, 0, 0);
            this.followButton.MouseLeftClick += this.OnFollowButtonClick;
            this.AddChild(this.followButton);
        }

        private void UpdateTitle()
        {
            if (this.animal.IsResting)
            {
                var activity = GetString(World.Temperature <= -1 ? StringsForThingPanels.Hibernating : StringsForThingPanels.Sleeping);
                var text = $"{this.animal.DisplayName.ToUpperInvariant()} ({activity})";
                if (text.Length > 38) text = $"{this.animal.ShortName.ToUpperInvariant()} ({activity})";
                this.titleLabel.Text = text;
            }
            else this.titleLabel.Text = this.animal.DisplayName.ToUpperInvariant();
        }

        private void OnFollowButtonClick(object sender, MouseEventArgs e)
        {
            this.followButton.IsSelected = !this.followButton.IsSelected;
            this.IsContentChangedSinceDraw = true;
            this.FollowButtonClick?.Invoke(this, null);
        }

        public override void Update()
        {
            if (this.IsVisible && this.animal != null)
            {
                if (this.followButton.IsSelected != (GameScreen.Instance.CameraTrackingThing == this.animal))
                {
                    this.followButton.IsSelected = !this.followButton.IsSelected;
                    this.IsContentChangedSinceDraw = true;
                }

                if (this.animal.ThingType == ThingType.Tortoise) this.UpdateTitle();
            }

            base.Update();
        }

        protected static string GetString(StringsForThingPanels key)
        {
            return LanguageManager.Get<StringsForThingPanels>(key);
        }
    }
}
