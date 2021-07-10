namespace SigmaDraconis.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.UI;
    using Managers;
    using Shared;
    using World;
    using WorldInterfaces;

    public class ColonistPortraitButtonContainer : RenderTargetElement
    {
        private readonly Dictionary<int, ColonistPortraitButton> buttons = new Dictionary<int, ColonistPortraitButton>();
        private readonly Dictionary<int, ColonistPortraitTooltip> tooltips = new Dictionary<int, ColonistPortraitTooltip>();

        public ColonistPortraitButtonContainer(IUIElement parent, int y)
            : base(parent, 0, y, parent.W, Scale(48))
        {
            this.IsInteractive = false;
        }

        public override void Update()
        {
            var updateLayout = false;

            var colonists = World.GetThings<IColonist>(ThingType.Colonist);
            foreach (var colonistId in this.buttons.Keys.ToList())
            {
                if (colonists.All(t => t.Id != colonistId))
                {
                    this.RemoveChild(this.buttons[colonistId]);
                    this.buttons.Remove(colonistId);
                    if (TooltipParent.Instance.Children.Any(c => c == this.tooltips[colonistId])) TooltipParent.Instance.RemoveChild(this.tooltips[colonistId]);
                    this.tooltips.Remove(colonistId);
                    updateLayout = true;
                }
                else this.buttons[colonistId].IsSelected = PlayerWorldInteractionManager.SelectedThing?.Id == colonistId;
            }

            foreach (var colonist in colonists)
            {
                if (this.buttons.All(t => t.Key != colonist.Id))
                {
                    if (colonist.MainTile.ThingsPrimary.Any(t => t.ThingType == ThingType.LandingPod) && !colonist.IsMoving) continue;  // Still in landing pod

                    var btn = new ColonistPortraitButton(this, (this.W / 2) - Scale(16), 0, "Textures\\Misc\\ColonistPortrait", colonist.Id);
                    btn.LoadContent();
                    this.AddChild(btn);
                    this.buttons.Add(colonist.Id, btn);
                    updateLayout = true;

                    var tooltip = new ColonistPortraitTooltip(this, btn, colonist);
                    this.tooltips.Add(colonist.Id, tooltip);
                    TooltipParent.Instance.AddChild(tooltip);
                }
            }

            if (updateLayout) this.UpdateLayout();

            base.Update();
        }

        public override void ApplyScale()
        {
            this.W = this.Parent.W;
            this.H = Scale(48);
        }

        public override void ApplyLayout()
        {
            foreach (var child in this.Children)
            {
                child.X = this.Rescale(child.X);
                child.Y = this.Rescale(child.Y);
                child.ApplyScale();
                child.ApplyLayout();
            }

            this.UpdateLayout();
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        protected override void OnParentResized(int prevW, int prevH)
        {
            base.OnParentResized(prevW, prevH);
            this.UpdateLayout();
        }

        protected void UpdateLayout()
        {
            var x = (this.W / 2) - Scale(16);
            if (this.buttons.Count > 0) x -= (this.buttons.Count - 1) * Scale(18);

            foreach (var btn in this.buttons)
            {
                btn.Value.X = x;
                this.tooltips[btn.Key].X = x;
                x += Scale(36);
            }

            this.IsContentChangedSinceDraw = true;
        }
    }
}
