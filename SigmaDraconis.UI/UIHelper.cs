namespace SigmaDraconis.UI
{
    using Draconis.Shared;
    using Draconis.UI;
    using Microsoft.Xna.Framework;
    using Language;
    using System;

    internal static class UIHelper
    {
        public static SimpleTooltip AddSimpleTooltip(IUIElement groupParent, IUIElement attachTo, string title = "", string text = "", TextAlignment textAlignment = TextAlignment.TopCentre)
        {
            var tooltip = new SimpleTooltip(TooltipParent.Instance, attachTo, title, text, textAlignment);
            TooltipParent.Instance.AddChild(tooltip, groupParent);
            return tooltip;
        }

        public static SimpleTooltip AddSimpleTooltip<T>(IUIElement groupParent, IUIElement attachTo, T titleId) where T : struct, IConvertible
        {
            var tooltip = new SimpleTooltip(TooltipParent.Instance, attachTo, titleId);
            TooltipParent.Instance.AddChild(tooltip, groupParent);
            return tooltip;
        }

        public static IconButton AddIconButton(IUIElement parent, int xUnscaled, int yUnscaled, string texturePath, EventHandler<MouseEventArgs> clickHandler)
        {
            var button = new IconButton(parent, Scale(xUnscaled), Scale(yUnscaled), texturePath, 1f, true);
            parent.AddChild(button);
            button.MouseLeftClick += clickHandler;
            return button;
        }

        public static TextLabel AddTextLabel(IUIElement parent, int xUnscaled, int yUnscaled, string text = "")
        {
            var label = new TextLabel(parent, Scale(xUnscaled), Scale(yUnscaled), text, UIColour.DefaultText);
            parent.AddChild(label);
            return label;
        }

        public static TextLabel AddTextLabel(IUIElement parent, int xUnscaled, int yUnscaled, Color colour, string text = "")
        {
            var label = new TextLabel(parent, Scale(xUnscaled), Scale(yUnscaled), text, colour);
            parent.AddChild(label);
            return label;
        }

        public static TextLabel AddTextLabel(IUIElement parent, int xUnscaled, int yUnscaled, int wUnscaled, Color colour, string text = "")
        {
            var label = new TextLabel(parent, Scale(xUnscaled), Scale(yUnscaled), Scale(wUnscaled), Scale(18), text, colour);
            parent.AddChild(label);
            return label;
        }

        public static TextLabel AddTextLabel<T>(IUIElement parent, int xUnscaled, int yUnscaled, T textId) where T : struct, IConvertible
        {
            var label = new TextLabelAutoScaling(parent, xUnscaled, yUnscaled, textId, UIColour.DefaultText);
            parent.AddChild(label);
            return label;
        }

        public static TextLabel AddTextLabel<T>(IUIElement parent, int xUnscaled, int yUnscaled, int wUnscaled, T textId) where T : struct, IConvertible
        {
            var label = new TextLabelAutoScaling(parent, xUnscaled, yUnscaled, wUnscaled, 18, textId, UIColour.DefaultText);
            parent.AddChild(label);
            return label;
        }

        public static TextLabel AddTextLabel<T>(IUIElement parent, int xUnscaled, int yUnscaled, int wUnscaled, Color colour, T textId) where T : struct, IConvertible
        {
            var label = new TextLabelAutoScaling(parent, xUnscaled, yUnscaled, wUnscaled, 18, textId, colour);
            parent.AddChild(label);
            return label;
        }

        public static TextButton AddTextButton(IUIElement parent, int xUnscaled, int yUnscaled, int wUnscaled, string text = "", int hUnscaled = 18)
        {
            var button = new TextButton(parent, Scale(xUnscaled), Scale(yUnscaled), Scale(wUnscaled), Scale(hUnscaled), text);
            parent.AddChild(button);
            return button;
        }

        public static TextButton AddTextButton(IUIElement parent, int yUnscaled, StringsForButtons textId, int heightUnscaled = 18)
        {
            var button = new TextButtonWithLanguage(parent, Scale(yUnscaled), textId, heightUnscaled);
            parent.AddChild(button);
            return button;
        }

        public static int Scale(int coord)
        {
            return coord * UIStatics.Scale / 100;
        }
    }
}
