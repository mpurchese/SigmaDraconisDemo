namespace SigmaDraconis.Language
{
    using Settings;
    using Shared;

    public static class LanguageHelper
    {
        private static int currentLanguageId;
        private static string kwStr;
        private static string kwhStr;
        private static string centigrateStr;
        private static string fahrenheitStr;
        private static string mpsStr;
        private static string mphStr;
        private static string kphStr;

        public static string KW
        {
            get
            {
                if (kwStr == null || currentLanguageId != LanguageManager.CurrentLanguageId) HandleLanguageChange();
                return kwStr;
            }
        }

        public static string KWh
        {
            get
            {
                if (kwStr == null || currentLanguageId != LanguageManager.CurrentLanguageId) HandleLanguageChange();
                return kwhStr;
            }
        }

        public static string GetForButton(object value)
        {
            return LanguageManager.Get<StringsForButtons>(value);
        }

        public static string GetForMouseCursor(object value)
        {
            return LanguageManager.Get<StringsForMouseCursor>(value);
        }

        public static string GetForMouseCursor(object value, object arg0)
        {
            return LanguageManager.Get<StringsForMouseCursor>(value, arg0);
        }

        public static string GetForMouseCursor(object value, object arg0, object arg1)
        {
            return LanguageManager.Get<StringsForMouseCursor>(value, arg0, arg1);
        }

        public static string GetForMouseCursor(object value, object arg0, object arg1, object arg2)
        {
            return LanguageManager.Get<StringsForMouseCursor>(value, arg0, arg1, arg2);
        }

        public static string GetForMouseCursor(object value, object arg0, object arg1, object arg2, object arg3)
        {
            return LanguageManager.Get<StringsForMouseCursor>(value, arg0, arg1, arg2, arg3);
        }

        public static string GetForMouseCursor(object value, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            return LanguageManager.Get<StringsForMouseCursor>(value, arg0, arg1, arg2, arg3, arg4);
        }

        public static string FormatTemperature(int degreesCelsius)
        {
            return SettingsManager.TemperatureUnit == TemperatureUnit.F
                ? $"{degreesCelsius.ToFahrenheit()}{fahrenheitStr}"
                : $"{degreesCelsius}{centigrateStr}";
        }

        public static string FormatTemperature(double degreesCelsius)
        {
            return SettingsManager.TemperatureUnit == TemperatureUnit.F
                ? $"{(int)degreesCelsius.ToFahrenheit()}{fahrenheitStr}"
                : $"{(int)degreesCelsius}{centigrateStr}";
        }

        public static string FormatWind(int metersPerSecond)
        {
            switch (SettingsManager.SpeedUnit)
            {
                case SpeedUnit.Kph: return $"{metersPerSecond.ToKph()}{kphStr}";
                case SpeedUnit.Mph: return $"{metersPerSecond.ToMph()}{mphStr}";
            }

            return $"{metersPerSecond}{mpsStr}";
        }

        public static string FormatTime(int frameNumber)
        {
            var timeMinutes = frameNumber / 3600;
            var timeSeconds = (frameNumber % 3600) / 60;
            return $"{timeMinutes:D2}:{timeSeconds:D2}";
        }

        private static void HandleLanguageChange()
        {
            currentLanguageId = LanguageManager.CurrentLanguageId;
            kwStr = LanguageManager.Get<StringsForUnits>(StringsForUnits.kW);
            kwhStr = LanguageManager.Get<StringsForUnits>(StringsForUnits.kWh);
            centigrateStr = LanguageManager.Get<StringsForUnits>(StringsForUnits.C);
            fahrenheitStr = LanguageManager.Get<StringsForUnits>(StringsForUnits.F);
            mpsStr = LanguageManager.Get<StringsForUnits>(StringsForUnits.mps);
            mphStr = LanguageManager.Get<StringsForUnits>(StringsForUnits.mph);
            kphStr = LanguageManager.Get<StringsForUnits>(StringsForUnits.kph);
        }
    }
}
