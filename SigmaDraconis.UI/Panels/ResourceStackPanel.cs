namespace SigmaDraconis.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.UI;
    using Language;
    using Shared;
    using World;
    using WorldInterfaces;

    public class ResourceStackPanel : PanelLeft, IThingPanel
    {
        private readonly LeftRightPicker priorityPicker;

        protected IResourceStack stack;
        public IThing Thing
        {
            get { return this.stack; }
            set
            {
                this.stack = value as IResourceStack;
                this.priorityPicker.SelectedIndex = (int)this.stack.HaulPriority;
                this.UpdateTitle();
            }
        }

        public ResourceStackPanel(IUIElement parent, int y)
            : base(parent, y, Scale(320), Scale(80), "")
        {
            var priorityOptions = new List<StringsForThingPanels> { StringsForThingPanels.Priority0, StringsForThingPanels.Priority1, StringsForThingPanels.Priority2, StringsForThingPanels.Priority3, StringsForThingPanels.Priority4 };
            this.priorityPicker = new LeftRightEnumPicker<StringsForThingPanels>(this, 80, 50, 160, priorityOptions, 2);
            this.AddChild(this.priorityPicker);
        }

        public override void Update()
        {
            if (this.stack != null)
            {
                this.UpdateTitle();
                this.stack.HaulPriority = (WorkPriority)this.priorityPicker.SelectedIndex;
                if (this.stack.ItemCount == 0 && this.stack.TargetItemCount > 0)
                {
                    var blueprint = World.ConfirmedBlueprints.Values.FirstOrDefault(b => b.MainTile == this.stack.MainTile && b.ThingType == this.stack.ThingType);
                    if (blueprint != null && blueprint.AnimationFrame != this.stack.TargetItemCount)
                    {
                        blueprint.AnimationFrame = this.stack.TargetItemCount;
                        EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Updated, blueprint);
                    }
                }
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            this.UpdateTitle();
            base.HandleLanguageChange();
        }

        private void UpdateTitle()
        {
            if (this.stack != null)
            {
                var name = this.stack.DisplayName.ToUpperInvariant();
                this.titleLabel.Text = $"{name} ({this.stack.ItemCount})";
            }
        }

        protected static string GetString(StringsForThingPanels key)
        {
            return LanguageManager.Get<StringsForThingPanels>(key);
        }
    }
}
