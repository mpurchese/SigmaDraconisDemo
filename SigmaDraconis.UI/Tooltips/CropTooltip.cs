namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using System.Text;
    using Draconis.UI;
    using Config;
    using Language;
    using Settings;
    using Shared;

    public class CropTooltip : SimpleTooltip
    {
        public CropDefinition CropDefinition { get; private set; }
        private bool isLocked;
        private string lockedDescription;

        public CropTooltip(IUIElement parent, IUIElement attachedElement, CropDefinition definition)
            : base(parent, attachedElement, "")
        {
            this.SetDefinition(definition);
            SettingsManager.SettingsSaved += this.OnSettingsSaved;
        }

        public void SetDefinition(CropDefinition definition, string lockedDescription = "")
        {
            this.CropDefinition = definition;
            this.isLocked = lockedDescription != "";
            this.lockedDescription = lockedDescription;
            if (definition == null) this.SetText("");
            else
            {
                this.SetTitle(definition.DisplayNameLong.ToUpperInvariant());
                
                var sb = new StringBuilder(GetString((StringsForCropTooltip)definition.Id));
                sb.Append("||");
                sb.Append(GetString(StringsForCropTooltip.GrowTime, definition.HoursToGrow));
                sb.Append("|");

                if (SettingsManager.TemperatureUnit == TemperatureUnit.F)
                {
                    var unit = LanguageManager.Get<StringsForUnits>(StringsForUnits.F);
                    sb.Append(GetString(StringsForCropTooltip.Temperature, definition.MinTemp.ToFahrenheit(), definition.MaxTemp.ToFahrenheit(), unit));
                    sb.Append("|");
                    sb.Append(GetString(StringsForCropTooltip.OptimalTemperature, definition.MinGoodTemp.ToFahrenheit(), definition.MaxGoodTemp.ToFahrenheit(), unit));
                }
                else
                {
                    var unit = LanguageManager.Get<StringsForUnits>(StringsForUnits.C);
                    sb.Append(GetString(StringsForCropTooltip.Temperature, definition.MinTemp, definition.MaxTemp, unit));
                    sb.Append("|");
                    sb.Append(GetString(StringsForCropTooltip.OptimalTemperature, definition.MinGoodTemp, definition.MaxGoodTemp, unit));
                }

                if (this.isLocked) sb.Append($"||{lockedDescription}");

                this.SetText(sb.ToString());
            }
        }

        private void OnSettingsSaved(object sender, System.EventArgs e)
        {
            this.SetDefinition(this.CropDefinition, this.lockedDescription);    // Temperature units may have changed
        }

        protected override void HandleLanguageChange()
        {
            this.SetDefinition(this.CropDefinition, this.lockedDescription);
            base.HandleLanguageChange();
        }

        protected override Color GetColourForLine(int lineNumber)
        {
            return (this.isLocked && lineNumber == this.lineCount) ? UIColour.RedText : base.GetColourForLine(lineNumber);
        }

        protected static string GetString(StringsForCropTooltip key)
        {
            return LanguageManager.Get<StringsForCropTooltip>(key);
        }

        protected static string GetString(StringsForCropTooltip key, object arg1)
        {
            return LanguageManager.Get<StringsForCropTooltip>(key, arg1);
        }

        protected static string GetString(StringsForCropTooltip key, object arg1, object arg2, object arg3)
        {
            return LanguageManager.Get<StringsForCropTooltip>(key, arg1, arg2, arg3);
        }
    }
}
