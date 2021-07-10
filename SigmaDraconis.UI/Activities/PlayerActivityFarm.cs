namespace SigmaDraconis.UI
{
    using Config;
    using Language;
    using Shared;
    using World;
    using WorldInterfaces;

    public static class PlayerActivityFarm
    {
        private static bool canDoAction;

        public static int CropType;
        public static bool IsReplaceExisting;

        public static void HandleLeftClick()
        {
            Update();
            if (!canDoAction) return;
            var planter = GameScreen.Instance.HighlightedThing as IPlanter;
            planter.SetCrop(CropType, IsReplaceExisting && planter.CurrentCropTypeId > 0);
        }

        public static void Update()
        {
            canDoAction = false;
            for (int i = 0; i < 5; i++) MouseCursor.Instance.TextLine[i] = "";

            if (!(GameScreen.Instance.HighlightedThing is IPlanter planter) || !planter.IsReady || planter.IsDesignatedForRecycling) return;

            var thisCropDefinition = planter.CurrentCropTypeId > 0 ? CropDefinitionManager.GetDefinition(planter.CurrentCropTypeId) : null;
            var nextCropDefinition = planter.SelectedCropTypeId > 0 ? CropDefinitionManager.GetDefinition(planter.SelectedCropTypeId) : null;
            var thisCropName = thisCropDefinition?.DisplayNameLong ?? LanguageHelper.GetForMouseCursor(StringsForMouseCursor.None);
            var nextCropName = nextCropDefinition?.DisplayNameLong ?? LanguageHelper.GetForMouseCursor(StringsForMouseCursor.None);
            MouseCursor.Instance.TextLine[0] = planter.PlanterStatus == PlanterStatus.InProgress 
                ? LanguageHelper.GetForMouseCursor(StringsForMouseCursor.ThisCropPercent, thisCropName, (int)(planter.Progress * 100f))
                : LanguageHelper.GetForMouseCursor(StringsForMouseCursor.ThisCrop, thisCropName);
            MouseCursor.Instance.TextLineColour[0] = World.ClimateType == ClimateType.Snow ? UIColour.WhiteText : UIColour.DefaultText;
            MouseCursor.Instance.TextLine[1] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.NextCrop, nextCropName);
            MouseCursor.Instance.TextLineColour[1] = World.ClimateType == ClimateType.Snow ? UIColour.WhiteText : UIColour.DefaultText;
            MouseCursor.Instance.TextLine[2] = "";
            
            MouseCursor.Instance.TextLine[4] = "";

            if (CropType > 0)
            {
                var definition = CropDefinitionManager.GetDefinition(CropType);
                if (planter.ThingType == ThingType.PlanterHydroponics && !definition.CanGrowHydroponics)
                {
                    MouseCursor.Instance.TextLine[3] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.CantGrowHydroponics, definition.DisplayNameLong);
                    MouseCursor.Instance.TextLineColour[3] = UIColour.RedText;
                    return;
                }

                if (planter.ThingType == ThingType.PlanterStone && !definition.CanGrowSoil)
                {
                    MouseCursor.Instance.TextLine[3] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.CantGrowSoil, definition.DisplayNameLong);
                    MouseCursor.Instance.TextLineColour[3] = UIColour.RedText;
                    return;
                }
            }

            MouseCursor.Instance.TextLineColour[3] = UIColour.YellowText;

            if (IsReplaceExisting)
            {
                if (planter.SelectedCropTypeId == CropType && (planter.CurrentCropTypeId == 0 || planter.CurrentCropTypeId == CropType))
                {
                    MouseCursor.Instance.TextLine[3] = "";
                    return;
                }

                if (CropType == 0)
                {
                    MouseCursor.Instance.TextLine[3] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.CancelThisCrop);
                }
                else
                {
                    var definition = CropDefinitionManager.GetDefinition(CropType);
                    MouseCursor.Instance.TextLine[3]
                        = LanguageHelper.GetForMouseCursor(planter.CurrentCropTypeId > 0 ? StringsForMouseCursor.SetThisCrop : StringsForMouseCursor.SetNextCrop, definition.DisplayNameLong);
                }
            }
            else
            {
                if (planter.SelectedCropTypeId == CropType && !planter.RemoveCrop)
                {
                    MouseCursor.Instance.TextLine[3] = "";
                    return;
                }

                if (CropType == 0)
                {
                    MouseCursor.Instance.TextLine[3] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.CancelNextCrop);
                }
                else
                {
                    var definition = CropDefinitionManager.GetDefinition(CropType);
                    MouseCursor.Instance.TextLine[3]
                        = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.SetNextCrop, definition.DisplayNameLong);
                }
            }

            canDoAction = true;
        }
    }
}
