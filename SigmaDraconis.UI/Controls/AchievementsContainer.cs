namespace SigmaDraconis.UI
{
    using Draconis.Shared;
    using Draconis.UI;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;

    internal class AchievementsContainer : RenderTargetElement
    {
        private readonly VerticalScrollBar scrollBar;
        private readonly List<AchievementItem> items = new List<AchievementItem>();
        private DateTime lastScrollUpdateTime;
        private int scrollY;
        private readonly int itemsPerPage;

        public AchievementsContainer(IUIElement parent, int x, int y, int width, int height, int itemsPerPage)
            : base(parent, x, y, width, height, true)
        {
            this.IsInteractive = true; // For scrolling
            this.itemsPerPage = itemsPerPage;

            this.scrollBar = new VerticalScrollBar(this, this.W - Scale(20), 0, this.H, this.itemsPerPage)
            {
                MaxScrollPosition = 12,
                FractionVisible = 0.25f
            };

            this.AddChild(this.scrollBar);

            var itemY = 2;
            for (int i = 1; i <= 16; i++)
            {
                var item = new AchievementItem(this, Scale(2), Scale(itemY), i);
                this.AddChild(item);
                this.items.Add(item);
                itemY += 84;
            }

            this.backgroundColour = new Color(0, 0, 0, 64);
            this.borderColour = new Color(64, 64, 64, 255);
        }

        public override void Update()
        {
            if (this.lastScrollUpdateTime < DateTime.Now.AddMilliseconds(-35))
            {
                this.lastScrollUpdateTime = DateTime.Now;
                var nextScrollY = this.scrollBar.IsMouseDragging ? (int)(this.scrollBar.ScrollPositionExact * 84) : Mathf.Clamp(this.scrollBar.ScrollPosition * 84, this.scrollY - 12, this.scrollY + 12);
                if (nextScrollY != this.scrollY)
                {
                    this.scrollY = nextScrollY;
                    this.UpdateChildCoords();
                }
            }

            base.Update();
        }

        private void UpdateChildCoords()
        {
            var y = 2 - this.scrollY;
            foreach (var item in this.items)
            {
                item.Y = Scale(y);
                y += 84;
            }
        }
    }
}
