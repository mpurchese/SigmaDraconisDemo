namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Input;

    using Draconis.Input;
    using Draconis.UI;
    using Language;
    using Shared;

    using Managers;

    using World;
    using World.Buildings;
    using WorldInterfaces;
    using WorldControllers;

    public static class PlayerActivityPlaceStackingArea
    {
        public static ISmallTile BlueprintTargetTile { get; set; }
        public static ISmallTile MouseDragStartTile = null;
        public static ItemType CurrentItemTypeToBuild { get; set; }

        public static void Update()
        {
            //var prevVirtualBlueprints = Mouse.GetState().LeftButton == ButtonState.Pressed ? World.VirtualBlueprint.ToList() : new List<Blueprint>();

            BuildHelper.CanBuildReason = "";

            // Cursor text
            var texts = new List<string>();
            var colours = new List<Color>();

            if (!GameScreen.Instance.IsMouseOver)
            {
                if (UIStatics.CurrentMouseState.LeftButton == ButtonState.Released) BlueprintController.ClearVirtualBlueprint();
                return;
            }

            BlueprintController.ClearVirtualBlueprint();

            if (MouseWorldPosition.Tile != null)
            {
                BlueprintTargetTile = MouseWorldPosition.Tile;

                var tilesForBuild = new List<ISmallTile>();
                var tooBig = false;
                if (MouseDragStartTile != null && MouseDragStartTile != BlueprintTargetTile)
                {
                    var x1 = Math.Min(MouseDragStartTile.X, MouseWorldPosition.Tile.X);
                    var y1 = Math.Min(MouseDragStartTile.Y, MouseWorldPosition.Tile.Y);
                    var x2 = Math.Max(MouseDragStartTile.X, MouseWorldPosition.Tile.X);
                    var y2 = Math.Max(MouseDragStartTile.Y, MouseWorldPosition.Tile.Y);
                    if (x2 - x1 >= 5 || y2 - y1 >= 5) tooBig = true;
                    else
                    {
                        for (int y = y1; y <= y2; y++)
                        {
                            for (int x = x1; x <= x2; x++)
                            {
                                var tile = World.GetSmallTile(x, y);
                                if (CanPlaceInTile(tile))
                                {
                                    tilesForBuild.Add(tile);
                                }
                            }
                        }
                    }
                }
                else if (CanPlaceInTile(BlueprintTargetTile))
                {
                    tilesForBuild.Add(BlueprintTargetTile);
                }

                if (tooBig)
                {
                    texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.AreaTooLarge));
                    colours.Add(UIColour.OrangeText);
                }
                else
                {
                    foreach (var tile in tilesForBuild)
                    {
                        BlueprintController.AddVirtualBuilding(tile, ThingType.StackingArea, true, (int)CurrentItemTypeToBuild);
                    }
                }
            }

            for (int i = 0; i < 5; i++)
            {
                MouseCursor.Instance.TextLine[i] = texts.Count > i ? texts[i] : "";
                MouseCursor.Instance.TextLineColour[i] = colours.Count > i ? colours[i] : UIColour.DefaultText;
            }

            foreach (var blueprint in World.VirtualBlueprint)
            {
                EventManager.RaiseEvent(EventType.VirtualBlueprint, EventSubType.Added, blueprint);
            }

            return;
        }

        private static bool CanPlaceInTile(ISmallTile tile)
        {
            return tile?.TerrainType == TerrainType.Dirt && tile.ThingsPrimary.All(t
                => !t.Definition.BlocksConstruction
                || t is IMoveableThing
                || (t is IStackingArea s && s.ItemType != CurrentItemTypeToBuild)
                || Constants.ItemTypesByResourceStackType.ContainsKey(t.ThingType));
        }

        public static void HandleLeftClick()
        {
            Update();

            var blueprints = World.VirtualBlueprint;
            foreach (var blueprint in blueprints)
            {
                var existingArea = blueprint.MainTile.ThingsPrimary.OfType<IStackingArea>().FirstOrDefault();
                if (existingArea != null) World.RemoveThing(existingArea);

                var stackingArea = new StackingArea(blueprint.MainTile, (ItemType)blueprint.AnimationFrame) { WorkPriority = WorkPriority.Low };
                World.AddThing(stackingArea);
                stackingArea.UpdateStack();
            }

            if (blueprints.Any() && !KeyboardManager.IsShift)
            {
                PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
            }
        }
    }
}
