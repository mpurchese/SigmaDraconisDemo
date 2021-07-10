namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Microsoft.Xna.Framework;
    using System.Collections.Generic;
    using System.Linq;
    using Language;
    using World.Projects;

    public class ProjectTooltip : Tooltip
    {
        protected List<TextLabel> descriptionLabels = new List<TextLabel>();
        protected List<TextLabel> statusLabels = new List<TextLabel>();
        private Project project;

        private static int LetterSpace => 7 * UIStatics.Scale / 100;
        private static int LineHeight => 16 * UIStatics.Scale / 100;

        public ProjectTooltip(IUIElement parent, IUIElement attachedElement, Project project, string title)
            : base(parent, attachedElement, Scale(380), Scale(76), title)
        {
            this.project = project;

            this.ResetLabels();
            this.UpdateStatusLabels();

            this.UpdateWidthAndHeight();
            this.ApplyLayout();
        }

        private void ResetLabels()
        {
            var description = project.Description ?? "";
            var descriptionLines = new List<string>();
            descriptionLines.AddRange(description.Split('|'));

            this.W = Scale(380);
            this.H = Scale(76);

            foreach (var label in this.descriptionLabels) this.RemoveChild(label);
            foreach (var label in this.statusLabels) this.RemoveChild(label);

            this.descriptionLabels.Clear();
            this.statusLabels.Clear();

            var y = Scale(20);
            for (var i = 0; i < descriptionLines.Count; i++)
            {
                var label = new TextLabel(this, 0, y, this.W, Scale(20), descriptionLines[i], UIColour.DefaultText);
                this.descriptionLabels.Add(label);
                this.AddChild(label);
                var w = Scale(20) + (LineHeight * descriptionLines[i].Length);
                if (w > this.W) this.W = w;
                y += LineHeight;
            }

            for (var i = 0; i < (project.Id == 105 ? 4 : 1); i++)
            {
                var label = new TextLabel(this, 0, y + Scale(6), this.W, Scale(20), "", UIColour.RedText);
                this.statusLabels.Add(label);
                this.AddChild(label);
            }
        }

        public override void ApplyLayout()
        {
            var y = Scale(20);
            foreach (var label in this.descriptionLabels)
            {
                label.Y = y;
                label.W = this.W;
                label.H = Scale(20);
                y += LineHeight;
            }

            y += Scale(6);
            foreach (var label in this.statusLabels)
            {
                label.Y = y;
                label.W = this.W;
                label.H = Scale(20);
                y += LineHeight;
            }

            base.ApplyLayout();
        }

        protected override void UpdateWidthAndHeight()
        {
            var w = Scale(380);
            foreach (var label in this.descriptionLabels)
            {
                var w1 = Scale(20) + (LetterSpace * label.Text.Length);
                if (w1 > w) w = w1;
            }

            this.H = Scale(30) + ((this.descriptionLabels.Count() + this.statusLabels.Count(l => l.Text != "")) * LineHeight);
            this.W = w;
        }

        public override void Update()
        {
            base.Update();
            if (this.Bottom > GameScreen.Instance.H - 1) this.Y = GameScreen.Instance.H - this.H - 1;

            this.UpdateStatusLabels();
        }

        protected override void HandleLanguageChange()
        {
            this.SetTitle(this.project.DisplayName.ToUpperInvariant());
            this.ResetLabels();
            this.UpdateWidthAndHeight();
            this.ApplyLayout();
            base.HandleLanguageChange();
        }

        protected void UpdateStatusLabels()
        {
            var statusLabelCount = this.statusLabels.Count(l => l.Text != "");

            this.project = ProjectManager.GetDefinition(this.project.Id);

            if (this.project.IsDone)
            {
                this.statusLabels[0].Text = GetString(StringsForProjectTooltip.Complete);
                this.statusLabels[0].Colour = UIColour.GreenText;
                for (int i = 1; i < statusLabels.Count; i++) this.statusLabels[i].Text = "";
            }
            else if (this.project.RemainingWork < this.project.TotalWork)
            {
                this.statusLabels[0].Text = GetString(StringsForProjectTooltip.Progress, (int)(100 - (100.0 * project.RemainingWork / project.TotalWork)));
                this.statusLabels[0].Colour = UIColour.OrangeText;
                for (int i = 1; i < statusLabels.Count; i++) this.statusLabels[i].Text = "";
            }
            else
            {
                var lockingProjects = ProjectManager.LockingProjects(this.project.Id).ToList();
                if (lockingProjects.Any())
                { 
                    for (int i = 0; i < this.statusLabels.Count; i++)
                    {
                        this.statusLabels[i].Text = i < lockingProjects.Count ? GetString(StringsForProjectTooltip.Requires, lockingProjects[i].DisplayName.ToUpperInvariant()) : "";
                        this.statusLabels[i].Colour = UIColour.RedText;
                    }
                }
                else
                {
                    this.statusLabels[0].Text = GetString(StringsForProjectTooltip.Progress, 0);
                    this.statusLabels[0].Colour = UIColour.OrangeText;
                    for (int i = 1; i < statusLabels.Count; i++) this.statusLabels[i].Text = "";
                }
            }

            if (statusLabelCount != this.statusLabels.Count(l => l.Text != "")) this.UpdateWidthAndHeight();
        }

        protected override void DrawBaseLayer()
        {
            if (this.attachedElement.IsMouseOver && this.attachedElement.IsVisible && this.IsEnabled)
            {
                this.spriteBatch.Begin();
                this.spriteBatch.Draw(this.titleBackgroundTexture, new Rectangle(0, 0, this.W, LineHeight - 1), Color.White);
                this.spriteBatch.Draw(this.borderTexture, new Rectangle(0, 0, this.W, 1), Color.White);
                this.spriteBatch.Draw(this.borderTexture, new Rectangle(0, 0, 1, this.H), Color.White);
                this.spriteBatch.Draw(this.borderTexture, new Rectangle(0, this.H - 1, this.W, 1), Color.White);
                this.spriteBatch.Draw(this.borderTexture, new Rectangle(this.W - 1, 0, 1, this.H), Color.White);

                spriteBatch.End();
            }
        }

        protected static string GetString(StringsForProjectTooltip key)
        {
            return LanguageManager.Get<StringsForProjectTooltip>(key);
        }

        protected static string GetString(StringsForProjectTooltip key, object arg0)
        {
            return LanguageManager.Get<StringsForProjectTooltip>(key, arg0);
        }
    }
}
