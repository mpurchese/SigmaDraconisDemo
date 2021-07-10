namespace SigmaDraconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Draconis.UI;
    using IO;
    using Language;
    using Shared;

    public class FileDetailsTooltip : SimpleTooltip
    {
        public FileDetailsTooltip(IUIElement parent, IUIElement attachedElement, SaveGameDetail gameDetail)
            : base(parent, attachedElement, gameDetail.FileName)
        {
            string dateStr;
            var age = (DateTime.Now.Date - gameDetail.FileDate.Date).TotalDays;
            if (age == 0) dateStr = GetString(StringsForFileDetailsTooltip.Today);
            else if (age == 1) dateStr = GetString(StringsForFileDetailsTooltip.Yesterday);
            else if (age < 7)
            {
                var dayStr = LanguageManager.GetNumberOrDate($"WeekDay{(int)gameDetail.FileDate.DayOfWeek}");
                dateStr = GetString(StringsForFileDetailsTooltip.OnDay, dayStr);
            }
            else
            {
                var dayStr = LanguageManager.GetNumberOrDate($"nth{gameDetail.FileDate.Day}");
                var monthStr = LanguageManager.GetNumberOrDate($"MonthShort{gameDetail.FileDate.Month}");
                dateStr = gameDetail.FileDate.Year == DateTime.Now.Year 
                    ? GetString(StringsForFileDetailsTooltip.DateFormat, dayStr, monthStr)
                    : GetString(StringsForFileDetailsTooltip.DateFormatWithYear, dayStr, monthStr, gameDetail.FileDate.Year);
            }

            var day = (gameDetail.WorldTime.TotalHoursPassed / WorldTime.HoursInDay) + 1;
            var hour = (gameDetail.WorldTime.TotalHoursPassed % WorldTime.HoursInDay) + 1;

            var line1 = GetString(StringsForFileDetailsTooltip.DetailFormatDate, dateStr, gameDetail.FileDate.ToShortTimeString());
            var line2 = GetString(StringsForFileDetailsTooltip.DetailFormatWorldTime, day, hour);
            var line3 = GetString(StringsForFileDetailsTooltip.DetailFormatVersion, gameDetail.GameVersion);

            if (gameDetail.GameVersion?.IsCompatible == true)
            {
                var description = $"{line1}|{line2}|{line3}";
                this.SetText(description);
            }
            else
            {
                var line4 = GetString(StringsForFileDetailsTooltip.NotCompatible);
                var description = $"{line1}|{line2}|{line3}|{line4}";
                this.SetText(description);
            }
        }

        protected override Color GetColourForLine(int lineNumber)
        {
            return lineNumber == 4 ? UIColour.RedText : UIColour.DefaultText;
        }

        private static string GetString(StringsForFileDetailsTooltip key)
        {
            return LanguageManager.Get<StringsForFileDetailsTooltip>(key);
        }

        private static string GetString(StringsForFileDetailsTooltip key, object arg0)
        {
            return LanguageManager.Get<StringsForFileDetailsTooltip>(key, arg0);
        }

        private static string GetString(StringsForFileDetailsTooltip key, object arg0, object arg1)
        {
            return LanguageManager.Get<StringsForFileDetailsTooltip>(key, arg0, arg1);
        }

        private static string GetString(StringsForFileDetailsTooltip key, object arg0, object arg1, object arg2)
        {
            return LanguageManager.Get<StringsForFileDetailsTooltip>(key, arg0, arg1, arg2);
        }
    }
}
