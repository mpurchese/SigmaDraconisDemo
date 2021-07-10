namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using WorldInterfaces;

    public class MothershipColonistDetail : UIElementBase
    {
        private readonly MothershipColonistPortrait portrait;
        private readonly TextLabel titleLabel;
        private readonly TextLabel skillLabel;
        private readonly TextLabel perkLabel;
        private readonly List<TextLabel> storyLabels = new List<TextLabel>();
        private readonly TextRenderer textRenderer;
        private readonly TextButton wakeButton;
        private readonly Tooltip cannotWakeNonEngineerTooltip;
        private bool isWakeAllowed = true;
        private string cantWakeReason = "";

        public Color BackgroundColour { get; set; } = new Color(0, 0, 0, 64);
        public Color BorderColour { get; set; } = new Color(64, 64, 64, 255);
        public IColonistPlaceholder Colonist { get; private set; }

        public event EventHandler<MouseEventArgs> WakeClick;

        public MothershipColonistDetail(TextRenderer textRenderer, IUIElement parent, int x, int y, int w, int h, IColonistPlaceholder colonist) : base(parent, x, y, w, h)
        {
            this.Colonist = colonist;
            this.textRenderer = textRenderer;

            this.titleLabel = new TextLabel(this, 0, 0, w, Scale(14), colonist.Name.ToUpperInvariant(), UIStatics.DefaultTextColour);
            this.AddChild(this.titleLabel);

            this.portrait = new MothershipColonistPortrait(this, Scale(8), Scale(22), colonist);
            this.AddChild(this.portrait);

            this.skillLabel = new TextLabel(this, Scale(48), Scale(21), "", UIStatics.DefaultTextColour);
            this.AddChild(this.skillLabel);

            this.perkLabel = new TextLabel(this, Scale(48), Scale(39), "", UIStatics.DefaultTextColour);
            this.AddChild(this.perkLabel);

            this.wakeButton = new TextButton(this, (this.W / 2) - Scale(80), this.H - Scale(36), Scale(160), Scale(20), GetString(StringsForMothershipDialog.WakeButton, colonist.Name.ToUpperInvariant()))
            { TextColour = UIColour.GreenText, IsVisible = colonist.PlaceHolderStatus == ColonistPlaceholderStatus.InStasis };
            this.AddChild(this.wakeButton);
            this.wakeButton.MouseLeftClick += this.OnWakeButtonClick;

            this.cannotWakeNonEngineerTooltip = new SimpleTooltip(TooltipParent.Instance, this.wakeButton) { IsEnabled = false };
            TooltipParentForDialogs.Instance.AddChild(this.cannotWakeNonEngineerTooltip);
        }

        private void OnWakeButtonClick(object sender, MouseEventArgs e)
        {
            this.WakeClick?.Invoke(this, e);
        }

        public override void LoadContent()
        {
            this.UpdateStatsLabels();
            this.UpdateStoryLabels();

            this.CreateTexture();
            base.LoadContent();
        }

        public void SetColonist(IColonistPlaceholder colonist, bool canWake, string cantWakeReason = "")
        {
            this.isWakeAllowed = canWake;
            this.cantWakeReason = cantWakeReason;
            this.Colonist = colonist;
            this.portrait.SetColonist(colonist);
            this.titleLabel.Text = colonist.Name.ToUpperInvariant();
            this.UpdateStatsLabels();
            this.UpdateStoryLabels();
            this.UpdateButtons();
            if (canWake)
            {
                this.cannotWakeNonEngineerTooltip.IsEnabled = false;
            }
            else
            {
                this.cannotWakeNonEngineerTooltip.SetTitle(cantWakeReason);
                this.cannotWakeNonEngineerTooltip.IsEnabled = true;
            }
        }

        public void UpdateButtons()
        {
            if (!this.isWakeAllowed && this.cantWakeReason != "")
            {
                this.wakeButton.IsVisible = true;
                this.wakeButton.Text = GetString(StringsForMothershipDialog.WakeButton, this.Colonist.Name.ToUpperInvariant());
                this.wakeButton.IsEnabled = false;
            }
            else if (this.isWakeAllowed && this.Colonist.PlaceHolderStatus == ColonistPlaceholderStatus.InStasis)
            {
                this.wakeButton.IsVisible = true;
                this.wakeButton.Text = GetString(StringsForMothershipDialog.WakeButton, this.Colonist.Name.ToUpperInvariant());
                this.wakeButton.TextColour = UIColour.GreenText;
                this.wakeButton.IsEnabled = true;
            }
            else if (this.isWakeAllowed)
            {
                this.wakeButton.IsVisible = true;
                this.wakeButton.Text = GetString(StringsForMothershipDialog.CancelWakeButton);
                this.wakeButton.TextColour = UIColour.RedText;
                this.wakeButton.IsEnabled = true;
            }
            else
            {
                this.wakeButton.IsVisible = false;
            }
        }

        protected void UpdateStatsLabels()
        {
            var skill1Str = GetString(StringsForMothershipDialog.Skill);
            var perkStr = GetString(StringsForMothershipDialog.Perk);

            var skillType1 = LanguageManager.Get<StringsForSkillTypeDetail>((StringsForSkillTypeDetail)(int)Colonist.Skill);
            var perk = this.Colonist.Traits.Count > 0 ? LanguageManager.GetCardName(this.Colonist.Traits[0]) : "";

            this.skillLabel.Text = $"{skill1Str} {skillType1}";
            this.perkLabel.Text = $"{perkStr} {perk}";
        }

        protected void UpdateStoryLabels()
        {
            foreach (var label in this.storyLabels) this.RemoveChild(label);
            this.storyLabels.Clear();

            var lineLength = (this.W - Scale(16)) / UIStatics.TextRenderer.LetterSpace;

            var y = Scale(80);
            foreach (var str in this.Colonist.Story)
            {
                var lines = SplitLine(str, lineLength);
                foreach (var line in lines)
                {
                    this.storyLabels.Add(new TextLabel(this.textRenderer, this, Scale(8), y, this.W - Scale(16), Scale(14), line, UIColour.DefaultText));
                    y += Scale(14);
                }

                y += Scale(10);
            }

            if (this.Colonist.Skill == SkillType.Programmer)
            {
                this.storyLabels.Add(new TextLabel(this.textRenderer, this, Scale(8), y + Scale(4), this.W - Scale(16), Scale(14), GetString(StringsForMothershipDialog.ProgrammerInfo1), UIColour.YellowText));
                this.storyLabels.Add(new TextLabel(this.textRenderer, this, Scale(8), y + Scale(18), this.W - Scale(16), Scale(14), GetString(StringsForMothershipDialog.ProgrammerInfo2), UIColour.YellowText));
            }

            foreach (var label in this.storyLabels) this.AddChild(label);
        }

        private static List<string> SplitLine(string str, int lineLength)
        {
            // Break string into words then recombine up to our maximum line length
            var words = str.Split(' ');
            var lines = new List<string>();
            var line = "";
            foreach (var word in words)
            {
                if (line == "") line = word;
                else if (line.Length + word.Length + 1 > lineLength)
                {
                    lines.Add(line);
                    line = word;
                }
                else line = $"{line} {word}";
            }

            lines.Add(line);

            return lines;
        }

        protected void CreateTexture()
        {
            this.texture = new Texture2D(UIStatics.Graphics, 1, 1);
            this.texture.SetData(new Color[1] { Color.White });
        }

        protected override void DrawContent()
        {
            if (this.texture == null) this.CreateTexture();

            var r1 = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            var r2 = new Rectangle(this.RenderX, this.RenderY, this.W, Scale(14));

            this.spriteBatch.Begin();

            // Background
            this.spriteBatch.Draw(this.texture, r1, this.BackgroundColour);
            this.spriteBatch.Draw(this.texture, r2, this.BackgroundColour);   // Title background
            this.spriteBatch.Draw(this.texture, r2, this.BackgroundColour);   // Title background

            // Borders
            this.spriteBatch.Draw(this.texture, new Rectangle(r1.X, r1.Y, r1.Width, 1), this.BorderColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r1.X, r1.Y, 1, r1.Height), this.BorderColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r1.X, r1.Bottom - 1, r1.Width, 1), this.BorderColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r1.Right - 1, r1.Y, 1, r1.Height), this.BorderColour);

            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }

        private static string GetString(object value)
        {
            return LanguageManager.Get(typeof(StringsForMothershipDialog), value);
        }

        private static string GetString(object value, object arg0)
        {
            return LanguageManager.Get(typeof(StringsForMothershipDialog), value, arg0);
        }
    }
}
