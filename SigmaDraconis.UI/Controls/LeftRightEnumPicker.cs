namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Language;

    public class LeftRightEnumPicker<T> : LeftRightPicker where T : struct, IConvertible
    {
        private readonly List<T> options;

        public LeftRightEnumPicker(IUIElement parent, int x, int y, int width, int defaultOptionIndex, bool loop = false)
            : base(parent, Scale(x), Scale(y), Scale(width), Enum.GetValues(typeof(T)).Cast<T>().Select(o => LanguageManager.Get<T>(o)).ToList(), defaultOptionIndex, loop)
        {
            this.options = Enum.GetValues(typeof(T)).Cast<T>().ToList();
        }

        public LeftRightEnumPicker(IUIElement parent, int x, int y, int width, List<T> options, int defaultOptionIndex, bool loop = false)
            : base(parent, Scale(x), Scale(y), Scale(width), options.Select(o => LanguageManager.Get<T>(o)).ToList(), defaultOptionIndex, loop)
        {
            this.options = options;
        }

        protected override void HandleLanguageChange()
        {
            this.UpdateOptions(this.options.Select(o => LanguageManager.Get<T>(o)).ToList(), this.SelectedIndex);
            base.HandleLanguageChange();
        }
    }
}
