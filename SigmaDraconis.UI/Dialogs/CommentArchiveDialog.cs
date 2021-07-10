namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using World;
    using WorldInterfaces;

    public class CommentArchiveDialog : DialogBase
    {
        private readonly CommentListContainer commentListContainer;
        private readonly TextButton closeButton;
        private readonly LeftRightPicker colonistPicker;

        public event EventHandler<EventArgs> CloseClick;

        public CommentArchiveDialog(IUIElement parent)
            : base(parent, Scale(480), Scale(316), StringsForDialogTitles.CommentArchive)
        {
            this.IsVisible = false;

            this.colonistPicker = new LeftRightPicker(this, (this.W / 2) - Scale(130), Scale(20), Scale(260), new List<string>() { GetString(StringsForCommentArchiveDialog.AllColonists) }, 0)
            {
                Tags = new List<object> { 0 }
            };
            this.AddChild(this.colonistPicker);
            this.colonistPicker.SelectedIndexChanged += this.OnColonistPickerSelectedIndexChanged;

            this.commentListContainer = new CommentListContainer(this, Scale(8), Scale(46), this.W - Scale(16), Scale(228), 999, 6, false);
            this.AddChild(this.commentListContainer);

            this.closeButton = new TextButtonWithLanguage(this, (this.W / 2) - Scale(50), this.H - Scale(28), Scale(100), Scale(20), StringsForButtons.Close) { IsSelected = true };
            this.closeButton.MouseLeftClick += this.OnCloseClick;
            this.AddChild(this.closeButton);
        }

        public override void Show()
        {
            if (!this.IsVisible)
            {
                this.commentListContainer.ResetScrollPosition();

                var allColonists = World.GetThings<IColonist>(ThingType.Colonist).ToList();

                this.colonistPicker.Tags.Clear();
                this.colonistPicker.Tags.Add(0);
                this.colonistPicker.Tags.AddRange(allColonists.Select(a => a.Id).OfType<object>());

                var list = new List<string> { GetString(StringsForCommentArchiveDialog.AllColonists) };
                list.AddRange(allColonists.Select(a => $"{a.ShortName} - {GetSkillName(a)}"));
                this.colonistPicker.UpdateOptions(list, 0);
            }

            base.Show();
        }

        private void OnColonistPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            this.commentListContainer.ColonistId = (int)this.colonistPicker.SelectedTag;
            this.commentListContainer.ResetScrollPosition();
        }

        protected override void HandleEscapeKey()
        {
            this.CloseClick?.Invoke(this, new EventArgs());
            base.HandleEscapeKey();
        }

        protected override void HandleEnterOrSpaceKey()
        {
            this.CloseClick?.Invoke(this, new EventArgs());
            base.HandleEscapeKey();
        }

        private void OnCloseClick(object sender, MouseEventArgs e)
        {
            this.CloseClick?.Invoke(this, new EventArgs());
        }

        private static string GetString(StringsForCommentArchiveDialog key)
        {
            return LanguageManager.Get<StringsForCommentArchiveDialog>(key);
        }

        private static string GetSkillName(IColonist colonist)
        {
            return LanguageManager.Get<SkillType>(colonist.Skill);
        }
    }
}
