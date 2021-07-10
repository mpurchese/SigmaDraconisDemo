namespace SigmaDraconis.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Config;
    using Language;
    using WorldInterfaces;

    internal class ColonistDietDisplay : UIElementBase
    {
        private Color backgroundColour = new Color(0, 0, 0, 64);
        private Color titleBackgroundColour = new Color(0, 0, 0, 128);
        private Color borderColour = new Color(64, 64, 64, 255);
        private readonly TextLabel titleLabel;
        private readonly TextLabel titleLabel2;
        private readonly DietTooltip dietTooltip;
        private readonly HorizontalStack recentMealStack;
        private readonly ColonistPanelFoodIcon[] foodIcons;
        private readonly ColonistPanelFoodPreferenceDisplay foodPreferenceDisplay;

        public IColonist Colonist { get; set; }

        public ColonistDietDisplay(IUIElement parent, int x, int y)
            : base(parent, x, y, Scale(244), Scale(70))
        {
            var titleText1 = $"{LanguageManager.Get<StringsForColonistPanel>(StringsForColonistPanel.Diet)}: ";
            var titleText2 = $"{LanguageManager.Get<StringsForColonistPanel>(StringsForColonistPanel.Neutral)} (+0)";
            this.titleLabel = UIHelper.AddTextLabel(this, 0, 2, 244, UIColour.DefaultText, $"{titleText1}{new string(' ', titleText2.Length)}");
            this.titleLabel2 = UIHelper.AddTextLabel(this, 0, 2, 244, UIColour.GreenText, $"{new string(' ', titleText1.Length)}{titleText2}");

            this.dietTooltip = TooltipParent.Instance.AddTooltip(new DietTooltip(this.titleLabel), this.Parent);

            this.recentMealStack = this.AddChild(new HorizontalStack(this, 0, Scale(22), Scale(244), Scale(22), TextAlignment.MiddleCentre) { Spacing = 6 });

            this.foodIcons = new ColonistPanelFoodIcon[4];
            for (var i = 0; i < 4; i++) this.foodIcons[i] = this.recentMealStack.AddChild(new ColonistPanelFoodIcon(this.recentMealStack, 0, 0, Scale(20), Scale(20)));

            this.foodPreferenceDisplay = this.AddChild(new ColonistPanelFoodPreferenceDisplay(this, 0, Scale(50), Scale(244), Scale(20)));
        }

        public override void Update()
        {
            if (this.Colonist == null || this.Colonist.IsDead) this.foodPreferenceDisplay.Clear();
            else this.foodPreferenceDisplay.UpdateList(this.Colonist.GetFoodDisikes().ToList(), this.Colonist.GetFoodNeutral().Where(f => f.Id > 0).ToList(), this.Colonist.GetFoodLikes().ToList());

            var mealCount = 0;
            var opinionSum = 0;
            var mealTypes = new HashSet<int>();

            var index = 0;
            foreach (var tuple in this.Colonist.GetRecentMeals())
            {
                var def = CropDefinitionManager.GetDefinition(tuple.Item1);
                if (def != null)
                {
                    var opinion = this.Colonist.GetFoodOpinion(tuple.Item1);
                    this.foodIcons[index].SetMealType(def, opinion, tuple.Item2);
                    mealCount++;
                    opinionSum += opinion.GetValueOrDefault();
                    mealTypes.Add(tuple.Item1);
                }
                else this.foodIcons[index].SetMealType(null, null, null);

                index++;
                if (index >= 4) break;
            }

            for (int i = index; i < 4; i++) this.foodIcons[i].SetMealType(null, null, null);

            var likesHappiness = 0;
            if (opinionSum > 2) likesHappiness = 2;
            else if (opinionSum > 0) likesHappiness = 1;
            else if (opinionSum < -2) likesHappiness = -2;
            else if (opinionSum < 0) likesHappiness = -1;

            var varietyHappiness = 0;
            if (mealTypes.Count > 3) varietyHappiness = 2;
            else if (mealTypes.Count > 2) varietyHappiness = 1;
            else if (mealCount >= 3 && mealTypes.Count == 1) varietyHappiness = -1;

            var totalHappiness = likesHappiness + varietyHappiness;

            var titleText1 = $"{LanguageManager.Get<StringsForColonistPanel>(StringsForColonistPanel.Diet)}: ";
            var titleText2 = $"{GetDescription(totalHappiness)} ({Format(totalHappiness)})";
            this.titleLabel.Text = $"{titleText1}{new string(' ', titleText2.Length)}";
            this.titleLabel2.Text = $"{new string(' ', titleText1.Length)}{titleText2}";
            this.titleLabel2.Colour = GetColour(totalHappiness);

            this.dietTooltip.SetValues(totalHappiness, likesHappiness, varietyHappiness);

            base.Update();
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
            var r2 = new Rectangle(this.RenderX, this.RenderY, this.W, Scale(16) + 1);

            this.spriteBatch.Begin();

            // Background
            this.spriteBatch.Draw(this.texture, r1, this.backgroundColour);
            this.spriteBatch.Draw(this.texture, r2, this.titleBackgroundColour);

            // Borders
            this.spriteBatch.Draw(this.texture, new Rectangle(r1.X, r1.Y, r1.Width, 1), this.borderColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r1.X, r1.Y, 1, r1.Height), this.borderColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r1.X, r1.Bottom - 1, r1.Width, 1), this.borderColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r1.Right - 1, r1.Y, 1, r1.Height), this.borderColour);

            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }

        private static string Format(int number)
        {
            return number >= 0 ? $"+{number}" : number.ToString();
        }

        private static Color GetColour(int number)
        {
            if (number > 2) return UIColour.GreenText;
            if (number > 0) return UIColour.LightGreenText;
            if (number < -2) return UIColour.RedText;
            if (number < 0) return UIColour.YellowText;
            return UIColour.DefaultText;
        }

        private static string GetDescription(int number)
        {
            if (number > 2) return LanguageManager.Get<StringsForColonistPanel>(StringsForColonistPanel.VeryGood);
            if (number > 0) return LanguageManager.Get<StringsForColonistPanel>(StringsForColonistPanel.Good);
            if (number < -2) return LanguageManager.Get<StringsForColonistPanel>(StringsForColonistPanel.VeryBad);
            if (number < 0) return LanguageManager.Get<StringsForColonistPanel>(StringsForColonistPanel.Bad);
            return LanguageManager.Get<StringsForColonistPanel>(StringsForColonistPanel.Neutral);
        }
    }
}
