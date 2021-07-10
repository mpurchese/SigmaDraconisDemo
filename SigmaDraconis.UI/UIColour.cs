namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;

    public struct UIColour
    {
        public static Color ButtonBackground { get { return new Color(0, 0, 0, 100); } }
        public static Color ButtonBackgroundDark { get { return new Color(0, 0, 0, 140); } }
        public static Color RedText { get { return new Color(255, 48, 0); } }
        public static Color DarkRedText { get { return new Color(210, 0, 0); } }
        public static Color OrangeText { get { return new Color(255, 140, 0); } }
        public static Color YellowText { get { return new Color(200, 180, 0); } }
        public static Color GoldText { get { return Color.Gold; } }
        public static Color DarkGreenText { get { return new Color(0, 128, 0); } }
        public static Color GreenText { get { return new Color(0, 200, 0); } }
        public static Color LightGreenText { get { return new Color(100, 200, 100); } }
        public static Color LightRedText { get { return new Color(255, 218, 176); } }
        public static Color BlueText { get { return new Color(32, 128, 255); } }
        public static Color LightBlueText { get { return new Color(120, 200, 255); } }
        public static Color PaleBlueText { get { return Color.LightBlue; } }
        public static Color PinkText { get { return new Color(255, 128, 255); } }
        public static Color WhiteText { get { return new Color(255, 255, 255); } }
        public static Color DefaultText { get { return new Color(200, 200, 200); } }
        public static Color LightGrayText { get { return new Color(160, 160, 160); } }
        public static Color GrayText { get { return new Color(140, 140, 140); } }
        public static Color DarkGrayText { get { return new Color(120, 120, 120); } }
        public static Color ProgressBar { get { return Color.DarkOrange; } }
        public static Color StorageBar { get { return new Color(120, 100, 100); } }
        public static Color FoodStorageBar { get { return new Color(120, 130, 80); } }
        public static Color ItemsStorageBar { get { return new Color(100, 100, 100); } }
        public static Color HydrogenStorageBar { get { return new Color(90, 110, 145); } }
        public static Color WaterDisplay { get { return new Color(200, 220, 255); } }
        public static Color BorderDark { get { return new Color(64, 64, 64); } }
        public static Color BorderMedium { get { return new Color(96, 96, 96); } }
        public static Color BorderLight { get { return new Color(128, 128, 128); } }
        public static Color BuildingConstructionBar { get { return new Color(165, 165, 255); } }
        public static Color BuildingDeconstructionBar { get { return new Color(255, 64, 0); } }
        public static Color BuildingMaintenanceBar { get { return new Color(0, 212, 255); } }
        public static Color BuildingWorkBar { get { return new Color(255, 192, 0); } }
        public static Color WarningsDisplayText { get { return new Color(230, 200, 45); } }
        public static Color WarningsDisplayTextSnow { get { return new Color(255, 192, 45); } }
        public static Color Transparent { get { return new Color(0, 0, 0, 0); } }
    }
}
