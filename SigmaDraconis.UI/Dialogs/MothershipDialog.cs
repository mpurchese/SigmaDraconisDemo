namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Settings;
    using Shared;
    using World;
    using WorldControllers;
    using WorldInterfaces;

    public class MothershipDialog : DialogBase
    {
        private readonly TextLabel helpLabel;
        private readonly TextButton startButton;
        private readonly TextButton cancelButton;
        private readonly List<MothershipColonistSummary> summaries = new List<MothershipColonistSummary>();
        private MothershipColonistDetail detail;
        private readonly List<IColonistPlaceholder> colonists = new List<IColonistPlaceholder>();
        private IColonistPlaceholder colonistToWake;

        private Texture2D pixelTexture2;

        public event EventHandler<MouseEventArgs> StartClick;
        public event EventHandler<MouseEventArgs> CancelClick;

        public MothershipDialog(IUIElement parent)
            : base(parent, Scale(840), Scale(466), StringsForDialogTitles.Mothership)
        {
            this.IsVisible = false;
            this.titleLabel.Colour = UIColour.LightBlueText;

            this.helpLabel = UIHelper.AddTextLabel(this, 12, 24, 816, StringsForMothershipDialog.Help1);

            this.startButton = new TextButton(this, (this.W / 2) - Scale(80), this.H - Scale(34), Scale(160), Scale(20), LanguageHelper.GetForButton(StringsForButtons.Start)) { TextColour = UIColour.GreenText, IsEnabled = false };
            this.AddChild(this.startButton);
            this.startButton.MouseLeftClick += this.OnStartClick;

            this.cancelButton = new TextButtonWithLanguage(this, this.W - Scale(200), this.H - Scale(34), Scale(160), Scale(20), StringsForButtons.Cancel) { TextColour = UIColour.RedText, IsVisible = false };
            this.AddChild(this.cancelButton);
            this.cancelButton.MouseLeftClick += this.OnCancelClick;
        }

        private void OnStartClick(object sender, MouseEventArgs e)
        {
            if (this.colonistToWake != null && !this.colonistToWake.IsWakeCommitted) MothershipController.WakeColonist(this.colonistToWake);
            this.StartClick?.Invoke(sender, e);
        }

        private void OnCancelClick(object sender, MouseEventArgs e)
        {
            foreach (var colonist in colonists.Where(c => c.PlaceHolderStatus == ColonistPlaceholderStatus.Waking && !c.IsWakeCommitted))
            {
                colonist.PlaceHolderStatus = ColonistPlaceholderStatus.InStasis;
                colonist.IsWakeCommitted = false;
            }

            this.CancelClick?.Invoke(sender, e);
        }

        private void OnWakeClick(object sender, MouseEventArgs e)
        {
            if (!(sender is MothershipColonistDetail detail)) return;

            if (this.colonistToWake == detail.Colonist) this.colonistToWake = null;// Cancel
            else this.colonistToWake = detail.Colonist;
            
            foreach (var colonist in colonists.Where(c => c.PlaceHolderStatus.In(ColonistPlaceholderStatus.Waking, ColonistPlaceholderStatus.InStasis)))
            {
                colonist.PlaceHolderStatus = (colonist == this.colonistToWake) ? ColonistPlaceholderStatus.Waking : ColonistPlaceholderStatus.InStasis;
            }

            if (this.colonistToWake != null)
            {
                var timeToArrival = 660;
                if (this.colonists.Any(c => c.IsWakeCommitted)) timeToArrival = Constants.HoursToWakeColonist * 3600;
                colonistToWake.TimeToArrivalInFrames = timeToArrival;
            }

            foreach (var summary in this.summaries)
            {
                summary.UpdateStatusLabel();
            }

            // Cancel button will be invisible on a new game 
            this.startButton.IsEnabled = (this.colonistToWake != null) || this.cancelButton.IsVisible;
            this.cancelButton.IsEnabled = this.colonistToWake != null;

            detail.UpdateButtons();
        }

        private void UpdateContents()
        {
            for (int i = this.summaries.Count; i < Constants.MaxColonists && i < colonists.Count; i++)
            {
                var summary = new MothershipColonistSummary(this, Scale(12), Scale(44) + (i * Scale(38)), Scale(230), Scale(36), colonists[i]);
                this.AddChild(summary);
                this.summaries.Add(summary);
                summary.MouseLeftClick += this.OnSummaryClick;
            }

            for (int i = this.colonists.Count + 1; i < Constants.MaxColonists; i++) this.summaries[0].IsVisible = false;

            var selectedIndex = -1;
            for (int i = 0; i < this.colonists.Count; i++)
            {
                if (summaries[i].IsSelected) selectedIndex = i;
            }

            if (selectedIndex == -1)
            {
                selectedIndex = 0;
                summaries[0].IsSelected = true;
            }

            if (this.detail == null)
            {
                this.detail = new MothershipColonistDetail(textRenderer, this, Scale(244), Scale(44), Scale(584), Scale(378), colonists[selectedIndex]);
                this.AddChild(this.detail);
                this.detail.WakeClick += this.OnWakeClick;
            }
        }

        protected override void OnShown()
        {
            var isNewGame = World.WorldTime.FrameNumber == 0;
            this.colonists.Clear();
            this.colonists.AddRange(MothershipController.GetColonistPlaceholders());
            this.UpdateContents();

            // Can start or continue game if at least one colonist is active or waking
            var canStart = false;
            for (var i = 0; i < this.summaries.Count && i < this.colonists.Count; i++)
            {
                if (this.colonists[i].ActualColonistID.HasValue && (!(World.GetThing(this.colonists[i].ActualColonistID.Value) is IColonist c) || c.IsDead))
                {
                    this.colonists[i].PlaceHolderStatus = ColonistPlaceholderStatus.Dead;
                }

                this.summaries[i].SetColonist(this.colonists[i]);
                if (this.colonists[i].PlaceHolderStatus.In(ColonistPlaceholderStatus.Active, ColonistPlaceholderStatus.Waking)) canStart = true;
            }

            var firstColonist = this.colonists.FirstOrDefault(c => c.PlaceHolderStatus.In(ColonistPlaceholderStatus.InStasis, ColonistPlaceholderStatus.Waking));
            if (firstColonist == null) firstColonist = this.colonists.FirstOrDefault();
            if (firstColonist != null)
            {
                this.SetDetailColonist(firstColonist);
            }

            this.startButton.Text = LanguageHelper.GetForButton(isNewGame ? StringsForButtons.Start : StringsForButtons.Continue);
            this.startButton.IsEnabled = canStart;
            this.cancelButton.IsVisible = !isNewGame;
            if (MothershipController.TimeToArrival > 0)
            {
                // Waking
                this.helpLabel.Text = GetString(StringsForMothershipDialog.Help4, MothershipController.ArrivingColonistName ?? "", LanguageHelper.FormatTime(MothershipController.TimeToArrival));
                this.helpLabel.Colour = UIColour.GreenText;
            }
            else if (MothershipController.TimeUntilCanWake > 0)
            {
                // Can't wake yet...
                this.helpLabel.Text = GetString(StringsForMothershipDialog.Help3, LanguageHelper.FormatTime(MothershipController.TimeUntilCanWake));
                this.helpLabel.Colour = UIColour.YellowText;
            }
            else
            {
                this.helpLabel.Text = GetString(isNewGame ? StringsForMothershipDialog.Help1 : StringsForMothershipDialog.Help2);
                this.helpLabel.Colour = UIColour.DefaultText;
            }

            for (int i = 0; i < this.colonists.Count && i < this.summaries.Count; i++)
            {
                summaries[i].IsSelected = (firstColonist == this.colonists[i]);
            }

            this.IsContentChangedSinceDraw = true;

            base.OnShown();
        }

        public override void LoadContent()
        {
            this.pixelTexture2 = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] color2 = new Color[1] { new Color(0, 0, 0, 64) };
            this.pixelTexture2.SetData(color2);

            base.LoadContent();
        }

        protected override void DrawBaseLayer()
        {
            if (this.pixelTexture2 != null && this.IsVisible)
            {
                var r1 = new Rectangle(0, 0, this.W, Scale(14));
                var r2 = new Rectangle(Scale(12), this.detail.Y, this.W - Scale(24), this.detail.H);

                spriteBatch.Begin();
                spriteBatch.Draw(pixelTexture, r1, Color.White);
                spriteBatch.Draw(pixelTexture2, r2, Color.White);
                spriteBatch.End();
            }
        }

        private void OnSummaryClick(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < this.summaries.Count; i++)
            {
                var summary = this.summaries[i];
                if (summary == sender)
                {
                    if (!summary.IsSelected)
                    {
                        summary.IsSelected = true;
                        var colonist = this.colonists[i];
                        this.SetDetailColonist(colonist);
                    }
                }
                else summary.IsSelected = false;
            }

            foreach (var summary in this.summaries) summary.IsSelected = (summary == sender);
        }

        private void SetDetailColonist(IColonistPlaceholder placeholder)
        {
            if (MothershipController.TimeUntilCanWake > 0 && placeholder.PlaceHolderStatus == ColonistPlaceholderStatus.InStasis)
            {
                this.detail.SetColonist(placeholder, false, LanguageManager.Get<StringsForMothershipDialog>(StringsForMothershipDialog.CannotWakeYet, LanguageHelper.FormatTime(MothershipController.TimeUntilCanWake)));
            }
            else
            {
                var canWake = MothershipController.CanWakeColonist(placeholder, out string reason);
                this.detail.SetColonist(placeholder, canWake, reason);
            }
        }

        public override void HandleKeyRelease(Keys key)
        {
            if (this.cancelButton.IsVisible && this.cancelButton.IsEnabled)
            {
                // TODO: this code is copied and should be factored out
                var keyName = key.ToString();
                if (key == Keys.OemOpenBrackets) keyName = "[";
                else if (key == Keys.OemCloseBrackets) keyName = "]";
                else if (key == Keys.OemPlus || key == Keys.Add) keyName = "+";
                else if (key == Keys.OemMinus || key == Keys.Subtract) keyName = "-";

                if (key == Keys.Escape || SettingsManager.GetKeysForAction("Mothership").Contains(keyName)) this.CancelClick?.Invoke(this, null);
            }
        }

        protected override void HandleLanguageChange()
        {
            var continueStr = LanguageHelper.GetForButton(StringsForButtons.Continue);
            var startStr = LanguageHelper.GetForButton(StringsForButtons.Continue);
            var buttonWidth = Scale(((Math.Max(continueStr.Length, startStr.Length) * 7) + 36).Clamp(100, 300));
            this.startButton.Text = World.WorldTime.FrameNumber == 0 ? startStr : continueStr;
            this.startButton.X = (this.W - buttonWidth) / 2;
            this.startButton.W = buttonWidth;

            base.HandleLanguageChange();
        }

        private static string GetString(object value)
        {
            return LanguageManager.Get<StringsForMothershipDialog>(value);
        }

        private static string GetString(object value, object arg0)
        {
            return LanguageManager.Get<StringsForMothershipDialog>(value, arg0);
        }

        private static string GetString(object value, object arg0, object arg1)
        {
            return LanguageManager.Get<StringsForMothershipDialog>(value, arg0, arg1);
        }
    }
}
