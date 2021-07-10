namespace SigmaDraconis.UI
{
    using System.Collections.Generic;
    using Draconis.Shared;
    using Draconis.UI;
    using Shared;
    using World;
    using WorldControllers;

    public class WarningsDisplay : UIElementBase
    {
        private readonly List<TextLabel> labels = new List<TextLabel>();

        public WarningsDisplay(IUIElement parent, int x, int y, int width, int height)
            : base(parent, x, y, width, height)
        {
            this.IsInteractive = false;
            this.AnchorBottom = true;
            this.AnchorRight = true;
            this.AnchorLeft = false;
            this.AnchorTop = false;

            for (int i = 0; i < 10; i++)
            {
                var label = new TextLabel(this, 0, Scale(i * 20), width, Scale(20), "", UIColour.WarningsDisplayText, TextAlignment.TopRight, true);
                this.AddChild(label);
                this.labels.Add(label);
            }
        }

        public override void Update()
        {
            if (WarningsController.IsDisplayInvalidated)
            {
                var newWarnings = WarningsController.GetDisplayMessages();
                WarningsController.IsDisplayInvalidated = false;

                for (int i = 0; i < 10; i++)
                {
                    if (newWarnings.Count > 9 - i)
                    {
                        var type = newWarnings[9 - i].Type;
                        this.labels[i].Text = newWarnings[9 - i].Message;
                        if (type == WarningType.Hypothermia) this.labels[i].Colour = UIColour.RedText;
                        else if (type.In(WarningType.LowPower, WarningType.VeryUnhappy, WarningType.NoFoodStorage)) this.labels[i].Colour = UIColour.OrangeText;
                        else this.labels[i].Colour = World.ClimateType == ClimateType.Snow ? UIColour.WarningsDisplayTextSnow : UIColour.WarningsDisplayText;
                    }
                    else this.labels[i].Text = "";
                }
            }

            base.Update();
        }
    }
}
