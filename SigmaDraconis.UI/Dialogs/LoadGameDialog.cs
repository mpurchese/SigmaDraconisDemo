namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.Shared;
    using Draconis.UI;
    using IO;
    using Language;
    using Settings;

    public class LoadGameDialog : DialogBase
    {
        private readonly TextButton manualSavesButton;
        private readonly TextButton autoSavesButton;
        private readonly TextLabel infoLabel1;
        private readonly TextLabel infoLabel2;
        private readonly TextButton cancelButton;
        private VerticalScrollBar scrollBar;
        private List<SaveGameDetail> fileDetails;
        private readonly List<IUIElement> buttons = new List<IUIElement>();
        private readonly List<SimpleTooltip> tooltips = new List<SimpleTooltip>();
        private Texture2D pixelTexture2;

        public event EventHandler<EventArgs> LoadClick;
        public event EventHandler<EventArgs> CancelClick;
        public event EventHandler<EventArgs> DeleteClick;

        public string FileName => this.SelectedSaveName;
        public string SelectedSaveName { get; set; }
        public bool IsAutosave => this.autoSavesButton.IsHighlighted;

        public LoadGameDialog(IUIElement parent)
            : base(parent, Scale(454), Scale(302), StringsForDialogTitles.LoadGame)
        {
            this.IsVisible = false;

            this.manualSavesButton = new TextButton(this, (this.W / 2) - Scale(204), Scale(20), Scale(200), Scale(20), GetString(StringsForLoadGameDialog.ManualSaves)) { IsHighlighted = true };
            this.manualSavesButton.MouseLeftClick += this.OnManualSavesClick;
            this.AddChild(this.manualSavesButton);

            this.autoSavesButton = new TextButton(this, (this.W / 2) + Scale(4), Scale(20), Scale(200), Scale(20), GetString(StringsForLoadGameDialog.AutoSaves)) { TextColour = UIColour.LightGrayText };
            this.autoSavesButton.MouseLeftClick += this.OnAutoSavesClick;
            this.AddChild(this.autoSavesButton);

            var maxAutosaves = SettingsManager.GetSettingInt(SettingGroup.Misc, SettingNames.AutosaveMaxCount) ?? 8;
            this.infoLabel1 = new TextLabel(this, 0, this.H - Scale(64), this.W, Scale(16), GetString(StringsForLoadGameDialog.AutosaveInfo1, maxAutosaves), UIColour.LightGrayText) { IsVisible = false };
            this.AddChild(this.infoLabel1);

            this.infoLabel2 = new TextLabel(this, 0, this.H - Scale(48), this.W, Scale(16), GetString(StringsForLoadGameDialog.AutosaveInfo2), UIColour.LightGrayText) { IsVisible = false };
            this.AddChild(this.infoLabel2);

            this.cancelButton = UIHelper.AddTextButton(this, 274, StringsForButtons.Cancel, 20);
            this.cancelButton.MouseLeftClick += this.OnCancelClick;

            this.UpdateButtons();
        }

        public void UpdateButtons()
        {
            foreach (var button in this.buttons) this.RemoveChild(button);
            this.buttons.Clear();

            foreach (var tooltip in this.tooltips) tooltip.Parent.RemoveChild(tooltip);
            this.tooltips.Clear();

            var isAutosave = this.autoSavesButton.IsHighlighted;
            this.fileDetails = SaveGameManager.GetSaveFileDetails(isAutosave, out _).ToList();

            var startIndex = this.scrollBar?.ScrollPosition ?? 0;
            var nextY = Scale(54);
            var maxRows = this.IsAutosave ? 8 : 9;
            var w = this.W + 2 - (this.fileDetails.Count > maxRows ? Scale(168) : Scale(152));
            for (int i = startIndex; i < startIndex + maxRows && i < this.fileDetails.Count; i++)
            {
                var detail = this.fileDetails[i];
                var name = detail.FileName.Substring(0, Math.Min(detail.FileName.Length, w / UIStatics.TextRenderer.LetterSpace));
                var nameButton = new TextButton(this, Scale(20), nextY, w, Scale(20), name)
                {
                    BackgroundColour = new Color(0, 0, 0, 64),
                    BorderColour1 = new Color(64, 64, 64),
                    TextDisabledColour = detail.GameVersion?.IsCompatible == true ? UIColour.LightGrayText : UIColour.GrayText,
                    IsEnabled = false
                };

                var loadButton = new TextButton(this, nameButton.Right + Scale(4), nextY, Scale(86), Scale(20), GetString(StringsForLoadGameDialog.LoadGame))
                {
                    BackgroundColour = new Color(18, 18, 18, 128),
                    BorderColourMouseOver = new Color(192, 192, 192),
                    TextColour = Color.Green,
                    Tag = name,
                    IsEnabled = detail.GameVersion?.IsCompatible == true
                };

                var deleteButton = new IconButton(this, nameButton.Right + Scale(94), nextY, "Textures\\Icons\\Delete", 1f, true) { Tag = name };
                if (UIStatics.Content != null) deleteButton.LoadContent();

                if (this.Parent is ModalBackgroundBox)
                {
                    this.AddTooltip(TooltipParentForDialogs.Instance, nameButton, detail);
                    this.AddTooltip(TooltipParentForDialogs.Instance, loadButton, detail);
                    this.AddTooltip(TooltipParentForDialogs.Instance, deleteButton, detail);
                }
                else if (this.Parent is MenuScreen menuScreen)
                {
                    this.AddTooltip(menuScreen.TooltipParent, nameButton, detail);
                    this.AddTooltip(menuScreen.TooltipParent, loadButton, detail);
                    this.AddTooltip(menuScreen.TooltipParent, deleteButton, detail);
                }

                loadButton.MouseLeftClick += this.OnLoadClick;
                deleteButton.MouseLeftClick += this.OnDeleteClick;

                // Re-raise scroll events so that scrollbar picks them up.  Not necessary for nameButton as this is disabled and will pass through its events anyway.
                loadButton.MouseScrollDown += this.OnChildMouseScrollDown;
                loadButton.MouseScrollUp += this.OnChildMouseScrollUp;
                deleteButton.MouseScrollDown += this.OnChildMouseScrollDown;
                deleteButton.MouseScrollUp += this.OnChildMouseScrollUp;

                this.buttons.Add(nameButton);
                this.buttons.Add(loadButton);
                this.buttons.Add(deleteButton);

                this.AddChild(nameButton);
                this.AddChild(loadButton);
                this.AddChild(deleteButton);

                nextY += Scale(22);
            }

            if (this.fileDetails.Count > maxRows)
            {
                if (this.scrollBar == null)
                {
                    this.scrollBar = new VerticalScrollBar(this, this.W - Scale(30) - 1, Scale(47), this.H - Scale(this.IsAutosave ? 114 : 92), maxRows);
                    this.AddChild(this.scrollBar);
                    this.scrollBar.ScrollPositionChange += this.ScrollPositionChange;
                }
                else
                {
                    this.scrollBar.PageSize = maxRows;
                    this.scrollBar.H = this.H - Scale(this.IsAutosave ? 114 : 92);
                }

                this.scrollBar.IsVisible = true;
                this.scrollBar.FractionVisible = maxRows / (float)this.fileDetails.Count;
                this.scrollBar.MaxScrollPosition = this.fileDetails.Count - maxRows;
            }
            else if (this.scrollBar != null)
            {
                this.scrollBar.ScrollPosition = 0;
                this.scrollBar.IsVisible = false;
            }

            this.infoLabel1.IsVisible = this.IsAutosave;
            this.infoLabel2.IsVisible = this.IsAutosave;
        }

        public override void LoadContent()
        {
            this.pixelTexture2 = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] color2 = new Color[1] { new Color(0, 0, 0, 128) };
            this.pixelTexture2.SetData(color2);

            base.LoadContent();
        }

        public override void ApplyLayout()
        {
            foreach (var child in this.Children)
            {
                if (child == this.scrollBar)
                {
                    child.X = this.W - Scale(30) - 1;
                    child.Y = Scale(47);
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

        public override void Show()
        {
            this.UpdateContent();
            base.Show();
        }

        protected override void HandleLanguageChange()
        {
            this.UpdateContent();
            base.HandleLanguageChange();
        }

        protected override void HandleEscapeKey()
        {
            this.CancelClick?.Invoke(this, new EventArgs());
            base.HandleEscapeKey();
        }

        private void UpdateContent()
        {
            this.manualSavesButton.Text = GetString(StringsForLoadGameDialog.ManualSaves);
            this.autoSavesButton.Text = GetString(StringsForLoadGameDialog.AutoSaves);

            var maxAutosaves = SettingsManager.GetSettingInt(SettingGroup.Misc, SettingNames.AutosaveMaxCount) ?? 8;
            this.infoLabel1.Text = GetString(StringsForLoadGameDialog.AutosaveInfo1, maxAutosaves);
            this.infoLabel2.Text = GetString(StringsForLoadGameDialog.AutosaveInfo2);

            this.UpdateButtons();
        }

        private void ScrollPositionChange(object sender, EventArgs e)
        {
            this.UpdateButtons();
        }

        protected override void DrawBaseLayer()
        {
            if (this.IsVisible)
            {
                Rectangle r1 = new Rectangle(0, 0, this.W, Scale(14));
                Rectangle r3 = new Rectangle(Scale(12), Scale(46), this.W - Scale(24), this.H - Scale(this.IsAutosave ? 112 : 90));
                Rectangle r4 = new Rectangle(Scale(12), this.infoLabel1.Y, this.W - Scale(24), Scale(32));

                spriteBatch.Begin();
                spriteBatch.Draw(pixelTexture, r1, Color.White);
                spriteBatch.Draw(pixelTexture2, r3, Color.White);
                if (this.infoLabel1.IsVisible) spriteBatch.Draw(pixelTexture2, r4, Color.White);
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

        private void OnManualSavesClick(object sender, MouseEventArgs e)
        {
            this.manualSavesButton.IsHighlighted = true;
            this.autoSavesButton.IsHighlighted = false;
            this.manualSavesButton.TextColour = UIColour.DefaultText;
            this.autoSavesButton.TextColour = UIColour.LightGrayText;
            this.UpdateButtons();
        }

        private void OnAutoSavesClick(object sender, MouseEventArgs e)
        {
            this.manualSavesButton.IsHighlighted = false;
            this.autoSavesButton.IsHighlighted = true;
            this.manualSavesButton.TextColour = UIColour.LightGrayText;
            this.autoSavesButton.TextColour = UIColour.DefaultText;
            this.UpdateButtons();
        }

        private void OnDeleteClick(object sender, MouseEventArgs e)
        {
            var button = (sender as IconButton);
            this.SelectedSaveName = button.Tag;
            foreach (var tooltip in this.tooltips) tooltip.Parent.RemoveChild(tooltip);
            this.tooltips.Clear();
            this.DeleteClick?.Invoke(this, new EventArgs());
        }

        private void OnLoadClick(object sender, MouseEventArgs e)
        {
            var button = (sender as TextButton);
            this.SelectedSaveName = button.Tag;
            foreach (var tooltip in this.tooltips) tooltip.Parent.RemoveChild(tooltip);
            this.tooltips.Clear();
            this.LoadClick?.Invoke(this, new EventArgs());
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
            return LanguageManager.Get(typeof(StringsForLoadGameDialog), value);
        }

        private static string GetString(object value, object arg0)
        {
            return LanguageManager.Get(typeof(StringsForLoadGameDialog), value, arg0);
        }
    }
}
