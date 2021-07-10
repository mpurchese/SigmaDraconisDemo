namespace Draconis.UI
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Used for attaching tooltips to, so they always appear on top.  Singleton.
    /// </summary>
    public class TooltipParent : UIElementBase
    {
        private Tooltip currentActiveTooltip;
        private readonly List<Tooltip> ungroupedTooltips = new List<Tooltip>();
        private readonly Dictionary<IUIElement, List<Tooltip>> groupedTooltips = new Dictionary<IUIElement, List<Tooltip>>();
        private int frameCounter;

        public static TooltipParent Instance { get; private set; }

        public TooltipParent(IUIElement parent)
            : base(parent, 0, 0, parent.W, parent.H)
        {
            if (Instance == null)
            {
                Instance = this;
                this.IsInteractive = false;
            }
            else
            {
                throw new System.ApplicationException("TooltipParent instance already created");
            }
        }

        public override T AddChild<T>(T child)
        {
            if (child is Tooltip t) ungroupedTooltips.Add(t);
            return base.AddChild(child);
        }

        public Tooltip AddChild(Tooltip child, IUIElement groupParent)
        {
            if (groupedTooltips.ContainsKey(groupParent)) groupedTooltips[groupParent].Add(child);
            else groupedTooltips.Add(groupParent, new List<Tooltip> { child });
            return base.AddChild(child);
        }

        public T AddTooltip<T>(T tooltip, IUIElement groupParent) where T : Tooltip
        {
            if (groupedTooltips.ContainsKey(groupParent)) groupedTooltips[groupParent].Add(tooltip);
            else groupedTooltips.Add(groupParent, new List<Tooltip> { tooltip });
            return base.AddChild(tooltip);
        }

        public override void RemoveChild(IUIElement child)
        {
            if (child is Tooltip t)
            {
                this.ungroupedTooltips.Remove(t);
                foreach (var group in this.groupedTooltips.ToList())
                {
                    if (group.Value.Contains(t)) group.Value.Remove(t);
                }
            }

            base.RemoveChild(child);
        }

        public override void Draw()
        {
            if (this.currentActiveTooltip != null) this.currentActiveTooltip.Draw();
        }

        public override void Update()
        {
            // Full update at 20fps
            this.frameCounter++;

            var activeTooltip = this.currentActiveTooltip;
            if (this.frameCounter == 3)
            {
                this.frameCounter = 0;
                activeTooltip = this.ungroupedTooltips.FirstOrDefault(c => c.AttachedElement.IsMouseOver && c.AttachedElement.IsVisibleIncludeParents && c.IsEnabled);
                if (activeTooltip == null)
                {
                    foreach (var group in this.groupedTooltips.Where(g => g.Key.IsMouseOver && g.Key.IsVisibleIncludeParents))
                    {
                        activeTooltip = group.Value.FirstOrDefault(c => c.AttachedElement.IsMouseOver && c.AttachedElement.IsVisibleIncludeParents && c.IsEnabled);
                        if (activeTooltip != null) break;
                    }
                }
            }

            if (activeTooltip != null) activeTooltip.Update();
            if (activeTooltip != this.currentActiveTooltip)
            {
                if (currentActiveTooltip != null) this.currentActiveTooltip.Update();
                this.currentActiveTooltip = activeTooltip;
            }
        }
    }
}
