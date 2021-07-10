namespace SigmaDraconis.UI
{
    using Draconis.Shared;
    using Draconis.UI;
    using CheckList;
    using Language;

    public class ChecklistPanel : PanelRight
    {
        private readonly TickBoxTextButton hideCompletedButton;
        //private readonly TickBoxTextButton showAllButton;
        private readonly CheckListContainer checkListContainer;
        private readonly CheckItemDetailDisplay checkItemDetailDisplay;

        public int ActiveItemId => this.checkListContainer.ActiveItemId;

        public ChecklistPanel(IUIElement parent, int y)
            : base(parent, y, Scale(360), Scale(390), GetString(StringsForChecklistPanel.Title))
        {
            //var showAllStr = "Show ALL Items";
            //var showAllWidth = 28 + (showAllStr.Length * 7);
            //this.showAllButton = new TickBoxTextButton(this, Scale(8), Scale(18), Scale(showAllWidth), Scale(20), showAllStr);
            //this.AddChild(this.showAllButton);
            //this.showAllButton.MouseLeftClick += this.OnShowAllButtonClick;

            var hideCompletedStr = GetString(StringsForChecklistPanel.HideCompletedItems);
            var hideCompletedWidth = 28 + (hideCompletedStr.Length * 7);
            this.hideCompletedButton = new TickBoxTextButton(this, Scale(352 - hideCompletedWidth), Scale(18), Scale(hideCompletedWidth), Scale(20), hideCompletedStr) { IsTicked = true };
            this.AddChild(this.hideCompletedButton);
            this.hideCompletedButton.MouseLeftClick += this.OnHideCompletedButtonClick;

            this.checkListContainer = new CheckListContainer(this, Scale(8), Scale(40), Scale(344), Scale(134), 6);
            this.AddChild(this.checkListContainer);
            this.checkListContainer.ItemClick += this.OnItemClick;

            this.checkItemDetailDisplay = new CheckItemDetailDisplay(this, Scale(8), Scale(178), Scale(344), Scale(200));
            this.AddChild(this.checkItemDetailDisplay);
        }

        private void OnHideCompletedButtonClick(object sender, MouseEventArgs e)
        {
            this.hideCompletedButton.IsTicked = !this.hideCompletedButton.IsTicked;
            this.checkListContainer.HideCompletedItems = this.hideCompletedButton.IsTicked;
        }

        //private void OnShowAllButtonClick(object sender, MouseEventArgs e)
        //{
        //    this.showAllButton.IsTicked = !this.showAllButton.IsTicked;
        //    this.checkListContainer.ShowAllItems = this.showAllButton.IsTicked;
        //}

        protected override void HandleLanguageChange()
        {
            this.Title = GetString(StringsForChecklistPanel.Title);

            var hideCompletedStr = GetString(StringsForChecklistPanel.HideCompletedItems);
            var hideCompletedWidth = 28 + (hideCompletedStr.Length * 7);
            this.hideCompletedButton.Text = hideCompletedStr;
            this.hideCompletedButton.X = Scale(352 - hideCompletedWidth);
            this.hideCompletedButton.W = Scale(hideCompletedWidth);

            base.HandleLanguageChange();
        }

        private void OnItemClick(object sender, MouseEventArgs e)
        {
            var item = CheckListController.GetItem(this.checkListContainer.ActiveItemId);
            CheckListController.ActiveItemId = this.checkListContainer.ActiveItemId;
            item.IsRead = true;
            this.checkItemDetailDisplay.SetItem(item);
        }

        private static string GetString(object value)
        {
            return LanguageManager.Get<StringsForChecklistPanel>(value);
        }
    }
}
