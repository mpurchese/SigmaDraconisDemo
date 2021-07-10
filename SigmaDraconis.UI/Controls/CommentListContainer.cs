namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.UI;
    using Commentary;

    internal class CommentListContainer : RenderTargetElement
    {
        private readonly VerticalScrollBar scrollBar;
        private readonly List<CommentaryHistoryPopupItem> items = new List<CommentaryHistoryPopupItem>();

        private bool isAtBottom = true;
        private int commentIndex = 0;
        private int scrollingDirection = 0;
        private int scrollPixelOffset = 0;
        private readonly int itemsPerPage;
        private readonly int maxItems;
        private readonly bool showArchiveButton;
        private int colonistId;

        private int commentCountOnReset;
        private int prevCommentCount;

        private List<Comment> filteredHistoryCache = new List<Comment>();

        private List<Comment> GetFilteredHistory()
        {
            if (CommentaryController.CommentHistoryCount != prevCommentCount)
            {
                prevCommentCount = CommentaryController.CommentHistoryCount;
                filteredHistoryCache = this.ColonistId > 0
                    ? CommentaryController.CommentHistory.Where(c => c.ColonistId == this.ColonistId).ToList()
                    : CommentaryController.CommentHistory;
            }

            return filteredHistoryCache;
        }

        public int ColonistId
        {
            get { return this.colonistId; }
            set
            {
                if (value == this.colonistId) return;
                this.colonistId = value;
                this.prevCommentCount = 0;
            }
        }

        public event EventHandler<EventArgs> ArchiveButtonClick;

        public CommentListContainer(IUIElement parent, int x, int y, int width, int height, int maxItems, int itemsPerPage, bool showArchiveButton)
            : base(parent, x, y, width, height)
        {
            this.itemsPerPage = itemsPerPage;
            this.maxItems = maxItems;
            this.showArchiveButton = showArchiveButton;

            this.scrollBar = new VerticalScrollBar(this, this.W - Scale(20), Scale(2), this.H - Scale(2), itemsPerPage);
            this.scrollBar.ScrollPositionChange += this.OnScrollPositionChange;
            this.AddChild(this.scrollBar);

            for (int i = 0; i < itemsPerPage + 2; i++)
            {
                var item = new CommentaryHistoryPopupItem(this, 0, Scale((i * 38) - 36), this.W - Scale(20), Scale(34)) { IsVisible = i > 0 && i <= itemsPerPage };
                this.items.Add(item);
                this.AddChild(item);
            }

             if (showArchiveButton) this.items[1].ArchiveButtonClick += this.OnArchiveButtonClick;

            this.commentCountOnReset = CommentaryController.CommentHistoryCount;
        }


        public void ResetScrollPosition()
        {
            this.isAtBottom = true;
            this.scrollingDirection = 0;
            this.scrollPixelOffset = 0;

            var allHistoryCount = this.GetFilteredHistory().Count();

            var rowCount = Math.Min(this.maxItems + 1, allHistoryCount);
            this.commentIndex = rowCount - 1;
            this.scrollBar.MaxScrollPosition = rowCount - this.itemsPerPage;
            this.scrollBar.ScrollPosition = rowCount - this.itemsPerPage;

            this.commentCountOnReset = allHistoryCount;
        }

        public override void Update()
        {
            var filteredHistory = this.GetFilteredHistory();

            // We need to keep the first index constant, so the actual max items can grow
            var maxItemsActual = this.maxItems + filteredHistory.Count - this.commentCountOnReset;

            var comments = filteredHistory.Take(maxItemsActual).ToList();

            var canShowHistory = filteredHistory.Count > maxItemsActual && this.showArchiveButton;
            if (canShowHistory) comments.Add(null);  // Null entry at the start will host the Show Archive button

            comments.Reverse();

            var firstItem = this.items[0];
            var lastItem = this.items[this.items.Count - 1];

            if (this.scrollBar.IsMouseDragging)
            {
                firstItem.IsVisible = this.items[0].Y > Scale(-34);
                firstItem.SetComment(this.commentIndex >= this.itemsPerPage && this.commentIndex - this.itemsPerPage < comments.Count ? comments[this.commentIndex - this.itemsPerPage] : null, canShowHistory);
                lastItem.SetComment(this.commentIndex >= -1 && this.commentIndex + 1 < comments.Count ? comments[this.commentIndex + 1] : null);
                this.items[this.items.Count - 1].IsVisible = true;
            }
            else if (this.scrollingDirection > 0)
            {
                firstItem.IsVisible = true;
                firstItem.SetComment(this.commentIndex >= this.itemsPerPage && this.commentIndex - this.itemsPerPage < comments.Count ? comments[this.commentIndex - this.itemsPerPage] : null, canShowHistory);
                lastItem.IsVisible = false;
            }
            else if (this.scrollingDirection < 0)
            {
                firstItem.IsVisible = false;
                lastItem.SetComment(this.commentIndex >= -1 && this.commentIndex + 1 < comments.Count ? comments[this.commentIndex + 1] : null);
                lastItem.IsVisible = true;
            }
            else if (firstItem.IsVisible || lastItem.IsVisible)
            {
                firstItem.IsVisible = false;
                lastItem.IsVisible = false;
                this.IsContentChangedSinceDraw = true;
            }

            for (int i = 1; i < this.items.Count - 1; i++)
            {
                var j = this.itemsPerPage - i;
                this.items[i].SetComment(this.commentIndex >= j && this.commentIndex - j < comments.Count ? comments[this.commentIndex - j] : null, i == 1 && canShowHistory);
            }

            for (int i = 0; i < this.items.Count; i++) this.items[i].Y = Scale((i * 38) - 36) + Scale(this.scrollPixelOffset);

            var rowCount = comments.Count;
            if (rowCount > this.itemsPerPage)
            {
                this.scrollBar.FractionVisible = this.itemsPerPage / (float)rowCount;
                this.scrollBar.MaxScrollPosition = rowCount - this.itemsPerPage;
                if (this.isAtBottom) this.scrollBar.ScrollPosition = this.scrollBar.MaxScrollPosition;
            }
            else
            {
                this.scrollBar.MaxScrollPosition = 0;
                this.scrollBar.ScrollPosition = 0;
                this.scrollBar.FractionVisible = this.itemsPerPage / (float)rowCount;
            }

            var targetIndex = Math.Min(comments.Count - 1, this.scrollBar.ScrollPosition + this.itemsPerPage - 1);

            if (this.scrollBar.IsMouseDragging)
            {
                this.scrollPixelOffset = (int)(-38 * (this.scrollBar.ScrollPositionExact % 1));
                this.commentIndex = (int)this.scrollBar.ScrollPositionExact + this.itemsPerPage - 1;
                this.scrollingDirection = 0;
            }
            else if (this.scrollingDirection > 0)
            {
                this.scrollPixelOffset -= 4 * this.scrollingDirection;
                if (this.scrollPixelOffset <= 0)
                {
                    if (this.commentIndex < targetIndex)
                    {
                        this.scrollPixelOffset += 38;
                        this.commentIndex++;
                    }
                    else
                    {
                        this.scrollPixelOffset = 0;
                        this.scrollingDirection = 0;
                    }
                }
            }
            else if (this.scrollingDirection < 0)
            {
                this.scrollPixelOffset += -4 * this.scrollingDirection;
                if (this.scrollPixelOffset >= 0)
                {
                    if (this.commentIndex > targetIndex)
                    {
                        this.scrollPixelOffset -= 38;
                        this.commentIndex--;
                    }
                    else
                    {
                        this.scrollPixelOffset = 0;
                        this.scrollingDirection = 0;
                    }
                }
            }
            else if (this.commentIndex < targetIndex)
            {
                this.scrollingDirection = targetIndex - this.commentIndex;
                this.commentIndex++;
                this.scrollPixelOffset = 38;
            }
            else if (this.commentIndex > 0 && this.commentIndex > targetIndex)
            {
                this.scrollingDirection = targetIndex - this.commentIndex;
                this.commentIndex--;
                this.scrollPixelOffset = -38;
            }
            else
            {
                this.scrollingDirection = 0;
                this.scrollPixelOffset = 0;
            }

            base.Update();
        }

        private void OnScrollPositionChange(object sender, EventArgs e)
        {
            this.isAtBottom = this.scrollBar.ScrollPosition == this.scrollBar.MaxScrollPosition;
        }

        private void OnArchiveButtonClick(object sender, EventArgs e)
        {
            this.ArchiveButtonClick?.Invoke(this, e);
        }
    }
}
