namespace SigmaDraconis.UI
{
    using Draconis.Input;
    using Draconis.Shared;
    using Draconis.UI;
    using IO;
    using Language;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class SaveGameDialog : DialogBase
    {
        private readonly TextLabel overwriteWarningLabel;
        private readonly HorizontalStack buttonStack;
        private readonly TextButton saveButton;
        private readonly TextButton cancelButton;
        private readonly TextBox fileNameTextBox;
        private VerticalScrollBar scrollBar;
        private List<SaveGameDetail> existingSaveFileDetails;
        private readonly List<IUIElement> buttons = new List<IUIElement>();
        private readonly List<SimpleTooltip> tooltips = new List<SimpleTooltip>();
        private Texture2D pixelTexture2;
        private bool isCaseSensitive;

        public string FileName { get; private set; }
        public bool IsSaveForExit { get; set; }

        public event EventHandler<EventArgs> SaveClick;
        public event EventHandler<EventArgs> CancelClick;
        public event EventHandler<EventArgs> DeleteClick;

        public SaveGameDialog(IUIElement parent)
            : base(parent, Scale(360), Scale(300), StringsForDialogTitles.SaveGame)
        {
            this.IsVisible = false;

            UIHelper.AddTextLabel(this, 20, 28, StringsForSaveGameDialog.FileName);
            UIHelper.AddTextLabel(this, 20, 52, StringsForSaveGameDialog.ExistingSaveGames);
            this.overwriteWarningLabel = UIHelper.AddTextLabel(this, 0, 251, 360, UIColour.OrangeText, StringsForSaveGameDialog.ExistingWillBeOverwritten);
            this.overwriteWarningLabel.IsVisible = false;

            var fileNameStr = GetString(StringsForSaveGameDialog.FileName);
            this.fileNameTextBox = new TextBox(this, Scale(24 + (fileNameStr.Length * 7)), Scale(26), Scale(320 - (fileNameStr.Length * 7)), Scale(20)) { CharacterMask = "\\w|\\s" };
            this.fileNameTextBox.TextChange += this.OnFileNameChanged;
            this.fileNameTextBox.EnterPress += this.OnFileNameEnter;
            this.AddChild(this.fileNameTextBox);

            this.buttonStack = new HorizontalStack(this, 0, this.H - Scale(30), this.W, Scale(20), TextAlignment.MiddleCentre) { Spacing = 8 };
            this.AddChild(this.buttonStack);

            this.saveButton = new TextButton(this.buttonStack, (this.W * 1 / 4) - Scale(50), this.H - Scale(30), Scale(100), Scale(20), LanguageHelper.GetForButton(StringsForButtons.Save)) { IsEnabled = false, TextColour = UIColour.GreenText };
            this.saveButton.MouseLeftClick += this.OnSaveClick;
            this.buttonStack.AddChild(this.saveButton);

            this.cancelButton = new TextButtonWithLanguage(this.buttonStack, (this.W * 3 / 4) - Scale(50), this.H - Scale(30), Scale(100), Scale(20), StringsForButtons.Cancel);
            this.cancelButton.MouseLeftClick += this.OnCancelClick;
            this.buttonStack.AddChild(this.cancelButton);

            this.UpdateButtons();
        }

        protected override void HandleLanguageChange()
        {
            var fileNameStr = GetString(StringsForSaveGameDialog.FileName);
            this.fileNameTextBox.X = Scale(24 + (fileNameStr.Length * 7));
            this.fileNameTextBox.W = Scale(320 - (fileNameStr.Length * 7));
            this.fileNameTextBox.MaxLength = (this.fileNameTextBox.W - 4) / UIStatics.TextRenderer.LetterSpace;

            base.HandleLanguageChange();
        }

        protected override void HandleEscapeKey()
        {
            this.CancelClick?.Invoke(this, new EventArgs());
            base.HandleEscapeKey();
        }

        private void OnFileNameChanged(object sender, EventArgs e)
        {
            this.UpdateButtonSelections();
        }

        private void OnFileNameEnter(object sender, EventArgs e)
        {
            if (this.saveButton.IsEnabled) this.OnSaveClick(this, null);
        }

        private void UpdateButtonSelections()
        {
            bool anySelected = false;
            foreach (var button in this.buttons.OfType<TextButton>())
            {
                button.IsSelected = this.isCaseSensitive
                    ? button.Text == this.fileNameTextBox.Text
                    : button.Text.ToLowerInvariant() == this.fileNameTextBox.Text.ToLowerInvariant();
                anySelected |= button.IsSelected;
            }

            this.overwriteWarningLabel.IsVisible = this.isCaseSensitive
                ? this.existingSaveFileDetails.Any(f => f.FileName == this.fileNameTextBox.Text)
                : this.existingSaveFileDetails.Any(f => f.FileName.ToLowerInvariant() == this.fileNameTextBox.Text.ToLowerInvariant());

            this.saveButton.IsEnabled = this.fileNameTextBox.Text.Length > 0 && !this.fileNameTextBox.Text.StartsWith(" ") && !this.fileNameTextBox.Text.EndsWith(" ");
        }

        public void UpdateButtons()
        {
            this.saveButton.Text = LanguageHelper.GetForButton(this.IsSaveForExit ? StringsForButtons.SaveAndExit : StringsForButtons.Save);
            this.saveButton.W = Scale(IsSaveForExit ? 160 : 100);
            this.buttonStack.LayoutInvalidated = true;

            foreach (var button in this.buttons) this.RemoveChild(button);
            this.buttons.Clear();

            foreach (var tooltip in this.tooltips) tooltip.Parent.RemoveChild(tooltip);
            this.tooltips.Clear();

            this.existingSaveFileDetails = SaveGameManager.GetSaveFileDetails(false, out bool linuxFormat);
            if (linuxFormat) this.isCaseSensitive = true;

            var startIndex = this.scrollBar?.ScrollPosition ?? 0;
            var nextY = Scale(66);
            var w = this.W - (this.existingSaveFileDetails.Count > 8 ? Scale(84) : Scale(68));
            for (int i = startIndex; i < startIndex + 8 && i < this.existingSaveFileDetails.Count; i++)
            {
                var detail = this.existingSaveFileDetails[i];
                var name = detail.FileName.Substring(0, Math.Min(detail.FileName.Length, w / UIStatics.TextRenderer.LetterSpace));
                var nameButton = new TextButton(this, Scale(20), nextY, w, Scale(20), name)
                {
                    BackgroundColour = new Color(0, 0, 0, 64),
                    TextColour = Color.Gray
                };

                var deleteButton = new IconButton(this, nameButton.Right + Scale(2), nextY, "Textures\\Icons\\Delete", 1f, true) { Tag = name };
                if (UIStatics.Content != null) deleteButton.LoadContent();

                this.AddTooltip(TooltipParentForDialogs.Instance, nameButton, detail);
                this.AddTooltip(TooltipParentForDialogs.Instance, deleteButton, detail);

                nameButton.MouseLeftClick += this.OnNameButtonClick;
                deleteButton.MouseLeftClick += this.OnDeleteClick;

                this.buttons.Add(nameButton);
                this.buttons.Add(deleteButton);

                this.AddChild(nameButton);
                this.AddChild(deleteButton);

                // Re-raise scroll events so that scrollbar picks them up.
                nameButton.MouseScrollDown += this.OnChildMouseScrollDown;
                nameButton.MouseScrollUp += this.OnChildMouseScrollUp;
                deleteButton.MouseScrollDown += this.OnChildMouseScrollDown;
                deleteButton.MouseScrollUp += this.OnChildMouseScrollUp;

                nextY += Scale(22);
            }

            if (this.existingSaveFileDetails.Count > 8)
            {
                if (this.scrollBar == null)
                {
                    this.scrollBar = new VerticalScrollBar(this, this.W - Scale(30) - 1, Scale(59), Scale(188), 9);
                    this.AddChild(this.scrollBar);
                    this.scrollBar.ScrollPositionChange += this.ScrollPositionChange;
                }

                this.scrollBar.IsVisible = true;
                this.scrollBar.FractionVisible = 8 / (float)this.existingSaveFileDetails.Count;
                this.scrollBar.MaxScrollPosition = this.existingSaveFileDetails.Count - 8;
            }
            else if (this.scrollBar != null)
            {
                this.scrollBar.ScrollPosition = 0;
                this.scrollBar.IsVisible = false;
            }

            this.UpdateButtonSelections();

            KeyboardManager.FocusedElement = this.fileNameTextBox;
        }

        public override void ApplyLayout()
        {
            foreach (var child in this.Children)
            {
                if (child == this.scrollBar)
                {
                    child.X = this.W - Scale(30) - 1;
                    child.Y = Scale(59);
                }
                else if (child == this.overwriteWarningLabel)
                {
                    child.X = 0;
                    child.Y = this.H - Scale(49);
                }
                else
                {
                    child.X = this.Rescale(child.X);
                    child.Y = this.Rescale(child.Y);
                }

                child.ApplyScale();
                child.ApplyLayout();
            }

            foreach (var tooltip in this.tooltips)
            {
                tooltip.ApplyScale();
                tooltip.ApplyLayout();
            }

            this.appliedScale = UIStatics.Scale;
            this.IsContentChangedSinceDraw = true;
        }

        public override void LoadContent()
        {
            this.pixelTexture2 = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] color2 = new Color[1] { new Color(0, 0, 0, 128) };
            this.pixelTexture2.SetData(color2);

            base.LoadContent();
        }

        private void ScrollPositionChange(object sender, EventArgs e)
        {
            this.UpdateButtons();
        }

        protected override void DrawBaseLayer()
        {
            if (this.IsVisible)
            {
                Rectangle r2 = new Rectangle(0, 0, this.W, Scale(14));
                Rectangle r3 = new Rectangle(Scale(12), Scale(24), this.W - Scale(24), Scale(24));
                Rectangle r4 = new Rectangle(Scale(12), Scale(52), this.W - Scale(24), Scale(196));
                Rectangle r5 = new Rectangle(Scale(12), Scale(250), this.W - Scale(24), Scale(18));

                spriteBatch.Begin();
                spriteBatch.Draw(pixelTexture, r2, Color.White);
                spriteBatch.Draw(pixelTexture2, r3, Color.White);
                spriteBatch.Draw(pixelTexture2, r4, Color.White);
                spriteBatch.Draw(pixelTexture2, r5, Color.White);
                spriteBatch.End();
            }
        }

        private void AddTooltip(IUIElement parent, IUIElement attachedElement, SaveGameDetail detail)
        {
            if (detail == null || detail.GameVersion == null || detail.WorldTime == null) return;
            var tooltip = new FileDetailsTooltip(parent, attachedElement, detail);
            parent.AddChild(tooltip);
            this.tooltips.Add(tooltip);
        }

        private void OnNameButtonClick(object sender, MouseEventArgs e)
        {
            var button = (sender as TextButton);
            this.FileName = button.Text;
            this.fileNameTextBox.Text = button.Text;
            this.UpdateButtonSelections();
        }

        private void OnDeleteClick(object sender, MouseEventArgs e)
        {
            var button = (sender as IconButton);
            this.FileName = button.Tag;
            foreach (var tooltip in this.tooltips) tooltip.Parent.RemoveChild(tooltip);
            this.tooltips.Clear();
            this.DeleteClick?.Invoke(this, new EventArgs());
        }

        private void OnSaveClick(object sender, MouseEventArgs e)
        {
            this.FileName = this.fileNameTextBox.Text;
            foreach (var tooltip in this.tooltips) tooltip.Parent.RemoveChild(tooltip);
            this.tooltips.Clear();
            this.SaveClick?.Invoke(this, new EventArgs());
        }

        private void OnCancelClick(object sender, MouseEventArgs e)
        {
            foreach (var tooltip in this.tooltips) tooltip.Parent.RemoveChild(tooltip);
            this.tooltips.Clear();
            this.CancelClick?.Invoke(this, new EventArgs());
        }

        private void OnChildMouseScrollDown(object sender, MouseEventArgs e)
        {
            // Re-raise event so that scrollbar picks it up
            this.OnMouseScrollDown(e);
        }

        private void OnChildMouseScrollUp(object sender, MouseEventArgs e)
        {
            // Re-raise event so that scrollbar picks it up
            this.OnMouseScrollUp(e);
        }

        private static string GetString(object value)
        {
            return LanguageManager.Get<StringsForSaveGameDialog>(value);
        }
    }
}
