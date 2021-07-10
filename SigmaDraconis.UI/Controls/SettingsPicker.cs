namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.UI;
    using Language;

    internal class SettingsPicker<T> : UIElementBase
    {
        private const int pickerWidth = 150;

        private readonly TextLabel label;
        private readonly LeftRightPicker picker;
        private readonly StringsForSettingsDialog labelTextId;
        private readonly List<T> optionTextIds = new List<T>();

        public int SelectedIndex
        {
            get => this.picker.SelectedIndex;
            set { this.picker.SelectedIndex = value; }
        }

        public bool IsEnabled
        {
            get => this.picker.IsEnabled;
            set { this.picker.IsEnabled = value; }
        }

        public string SelectedText => this.picker.SelectedText;

        public event EventHandler<EventArgs> SelectedIndexChanged;

        public SettingsPicker(IUIElement parent, int x, int y, int w, StringsForSettingsDialog labelTextId, List<T> options, int defaultOptionIndex)
            : base(parent, x, y, w, Scale(18))
        {
            this.labelTextId = labelTextId;
            this.optionTextIds.AddRange(options);

            this.label = new TextLabel(this, 0, Scale(2), LanguageManager.Get<StringsForSettingsDialog>(labelTextId), UIColour.DefaultText);
            this.picker = new LeftRightPicker(this, w - Scale(pickerWidth), 0, Scale(pickerWidth), options.Select(o => LanguageManager.Get<T>(o)).ToList(), defaultOptionIndex);
            this.AddChild(this.label);
            this.AddChild(this.picker);
            this.picker.SelectedIndexChanged += this.OnPickerSelectedIndexChanged;
        }

        public SettingsPicker(IUIElement parent, int x, int y, int w, StringsForSettingsDialog labelTextId, List<string> options, int defaultOptionIndex)
            : base(parent, x, y, w, Scale(18))
        {
            this.labelTextId = labelTextId;

            this.label = new TextLabel(this, 0, Scale(2), LanguageManager.Get<StringsForSettingsDialog>(labelTextId), UIColour.DefaultText);
            this.picker = new LeftRightPicker(this, w - Scale(pickerWidth), 0, Scale(pickerWidth), options, defaultOptionIndex);
            this.AddChild(this.label);
            this.AddChild(this.picker);
            this.picker.SelectedIndexChanged += this.OnPickerSelectedIndexChanged;
        }

        public void SetWidth(int w)
        {
            this.W = w;
            this.picker.X = w - Scale(pickerWidth);
        }

        public void UpdateOptions(List<string> options, int defaultOptionIndex)
        {
            this.picker.UpdateOptions(options, defaultOptionIndex);
        }

        protected override void HandleLanguageChange()
        {
            this.label.Text = LanguageManager.Get<StringsForSettingsDialog>(this.labelTextId);
            if (optionTextIds.Any()) this.picker.UpdateOptions(this.optionTextIds.Select(o => LanguageManager.Get<T>(o)).ToList(), this.picker.SelectedIndex);

            base.HandleLanguageChange();
        }

        protected void OnPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            this.SelectedIndexChanged?.Invoke(this, e);
        }
    }
}
