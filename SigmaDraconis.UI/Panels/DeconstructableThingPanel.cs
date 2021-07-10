namespace SigmaDraconis.UI
{
    using System;
    using Draconis.UI;
    using Language;
    using Shared;
    using WorldInterfaces;

    public class DeconstructableThingPanel : PanelLeft, IThingPanel
    {
        protected readonly TextLabel generalInfoLabel;
        protected readonly WorkPriorityTextButton deconstructButton;


        protected IRecyclableThing thing;
        public IThing Thing
        {
            get { return this.thing; }
            set
            {
                this.thing = value as IRecyclableThing;
                this.titleLabel.Text = value.DisplayName.ToUpperInvariant();
            }
        }

        public DeconstructableThingPanel(IUIElement parent, int y)
            : base(parent, y, Scale(320), Scale(100), "")
        {
            this.generalInfoLabel = new TextLabel(this, 0, Scale(24), Scale(320), Scale(22), "", UIColour.DefaultText);
            this.AddChild(this.generalInfoLabel);

            this.deconstructButton = new WorkPriorityTextButton(this, Scale(60), Scale(22), Scale(200), "");
            this.deconstructButton.PriorityChanged += this.OnDeconstructPriorityChanged;
            this.AddChild(this.deconstructButton);
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
                }
                else
                {
                    this.deconstructButton.IsVisible = true;
                    this.generalInfoLabel.Text = "";

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
                }
            }

            base.Update();
        }

        protected void OnDeconstructPriorityChanged(object sender, EventArgs e)
        {
            if (this.thing == null) return;

            if (this.deconstructButton.PriorityLevel == WorkPriority.Disabled || this.thing.RecyclePriority == WorkPriority.Disabled)
            {
                // Toggles deconstruction
                PlayerActivityDeconstruct.Deconstruct(this.thing);
            }

            this.thing.RecyclePriority = this.deconstructButton.PriorityLevel;
        }
    }
}
