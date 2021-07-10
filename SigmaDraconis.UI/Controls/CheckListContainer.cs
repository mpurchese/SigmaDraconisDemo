namespace SigmaDraconis.UI
{
    using CheckList;
    using Draconis.Shared;
    using Draconis.UI;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class CheckListContainer : RenderTargetElement
    {
        private readonly VerticalScrollBar scrollBar;
        private readonly List<CheckListItem> items = new List<CheckListItem>();
        private readonly Dictionary<int, CheckListItem> itemsMap = new Dictionary<int, CheckListItem>();
        private bool isFirstUpdate = true;
        private DateTime lastUpdateTime;
        private DateTime lastScrollUpdateTime;
        private int scrollY;
        private readonly int itemsPerPage;
        private bool hideCompletedItems;
        private bool showAllItems;
        private bool isReset;

        public int ActiveItemId { get; private set; }

        public bool HideCompletedItems {
            get => this.hideCompletedItems;
            set
            {
                if (value != this.hideCompletedItems)
                {
                    this.hideCompletedItems = value;
                    this.isReset = true;
                }
            }
        }

        public bool ShowAllItems
        {
            get => this.showAllItems;
            set
            {
                if (value != this.showAllItems)
                {
                    this.showAllItems = value;
                    this.isReset = true;
                }
            }
        }

        public event EventHandler<MouseEventArgs> ItemClick;

        public CheckListContainer(IUIElement parent, int x, int y, int width, int height, int itemsPerPage)
            : base(parent, x, y, width, height, true)
        {
            this.IsInteractive = true; // For scrolling
            this.itemsPerPage = itemsPerPage;
            this.hideCompletedItems = true;

            this.scrollBar = new VerticalScrollBar(this, this.W - Scale(20), 0, this.H, this.itemsPerPage);
            this.AddChild(this.scrollBar);

            this.backgroundColour = new Color(0, 0, 0, 64);
            this.borderColour = new Color(64, 64, 64, 255);
        }

        public override void Update()
        {
            var newItems = this.showAllItems 
                ? CheckListController.GetAllItems(out bool isLanguageChanged) 
                : CheckListController.GetItemsForDisplay(this.hideCompletedItems, out isLanguageChanged);

            if (CheckListController.IsReset || this.isReset || newItems.Count < this.items.Count)
            {
                CheckListController.IsReset = false;
                this.isReset = false;
                this.isFirstUpdate = true;
                this.lastScrollUpdateTime = new DateTime();
                this.lastUpdateTime = new DateTime();
                this.scrollBar.ScrollPosition = 0;
                this.scrollY = 0;
                foreach (var item in this.items) this.RemoveChild(item);
                this.items.Clear();
                this.itemsMap.Clear();
            }
            else if (isLanguageChanged)
            {
                foreach (var item in newItems.Where(i => this.itemsMap.ContainsKey(i.Id)))
                {
                    var itemUi = this.itemsMap[item.Id];
                    itemUi.Text = item.Title;
                }
            }

            if (this.lastScrollUpdateTime < DateTime.Now.AddMilliseconds(-35))
            {
                this.lastScrollUpdateTime = DateTime.Now;
                var nextScrollY = this.scrollBar.IsMouseDragging ? (int)(this.scrollBar.ScrollPositionExact * 22) : Mathf.Clamp(this.scrollBar.ScrollPosition * 22, this.scrollY - 5, this.scrollY + 5);
                if (nextScrollY != this.scrollY)
                {
                    this.scrollY = nextScrollY;
                    this.UpdateChildCoords();
                }
            }

            if (this.lastUpdateTime < DateTime.Now.AddSeconds(-1))
            {
                this.lastUpdateTime = DateTime.Now;

                if (this.isFirstUpdate) this.ActiveItemId = CheckListController.ActiveItemId;

                var haveNewItem = false;
                foreach (var item in newItems)
                {
                    if (!this.itemsMap.ContainsKey(item.Id))
                    {
                        var itemUi = new CheckListItem(this, Scale(2), Scale(2 + (this.items.Count * 22) - this.scrollY), this.W - Scale(24), Scale(20), item.Title, item.Id) 
                        { 
                            IsChecked = item.IsComplete,
                            IsNew = !item. IsRead && !item.IsComplete 
                        };

                        this.items.Add(itemUi);
                        this.itemsMap.Add(item.Id, itemUi);
                        this.AddChild(itemUi);
                        haveNewItem = true;
                        itemUi.Click += this.OnItemClick;
                        itemUi.MouseScrollDown += this.OnItemScrollDown;
                        itemUi.MouseScrollUp += this.OnItemScrollUp;
                    }
                    else
                    {
                        var itemUi = this.itemsMap[item.Id];
                        itemUi.IsChecked = item.IsComplete;
                        if (item.IsComplete && itemUi.IsNew) itemUi.IsNew = false;
                    }
                }

                this.scrollBar.MaxScrollPosition = Math.Max(0, this.items.Count - this.itemsPerPage);
                this.scrollBar.FractionVisible = Math.Min(1, this.itemsPerPage / (float)this.items.Count);

                if (haveNewItem && !this.scrollBar.IsMouseDragging) this.AutoScroll();
                if (this.isFirstUpdate)
                {
                    this.isFirstUpdate = false;
                    this.scrollY = this.scrollBar.ScrollPosition * 22;
                    this.UpdateChildCoords();
                    if (this.itemsMap.ContainsKey(this.ActiveItemId)) this.OnItemClickInner();
                    else if (this.ActiveItemId > 0)
                    {
                        // Uncomment to hide item if complete
                        //this.ActiveItemId = 0;
                        //this.ItemDeselected?.Invoke(this, null);
                    }
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
                y += 22;
            }
        }

        private void AutoScroll()
        {
            // Scroll to bottom or until selected item is at top
            var targetScrollPos = Math.Max(0, this.items.Count - this.itemsPerPage);
            var selectedIndex = this.items.FindIndex(i => i.IsSelected);
            if (selectedIndex >= 0 && targetScrollPos > selectedIndex) targetScrollPos = selectedIndex;
            this.scrollBar.ScrollPosition = targetScrollPos;
        }

        private void OnItemScrollDown(object sender, MouseEventArgs e)
        {
            this.OnMouseScrollDown(e);
        }

        private void OnItemScrollUp(object sender, MouseEventArgs e)
        {
            this.OnMouseScrollUp(e);
        }

        private void OnItemClick(object sender, MouseEventArgs e)
        {
            this.ActiveItemId = (sender as CheckListItem)?.ItemId ?? 0;
            this.OnItemClickInner();
        }

        private void OnItemClickInner()
        {
            foreach (var kv in this.itemsMap)
            {
                kv.Value.IsSelected = kv.Key == this.ActiveItemId;
                if (kv.Key == this.ActiveItemId) kv.Value.IsNew = false;
            }

            this.ItemClick?.Invoke(this, null);
        }
    }
}
