namespace SigmaDraconis.UI.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Xna.Framework.Input;

    using Draconis.Input;
    using Draconis.Shared;
    using Draconis.UI;
    using Config;
    using Shared;

    using World;
    using World.Buildings;
    using WorldInterfaces;
    using WorldControllers;

    public static class PlayerWorldInteractionManager
    {
        private static Vector2f scrollPosition = new Vector2i(0, 0);
        private static float zoom;
        private static PlayerActivityType currentActivity = PlayerActivityType.None;
        private static PlayerActivitySubType currentActivitySubType = PlayerActivitySubType.None;
        private static int prevMetal = 0;
        private static int prevStone = 0;

        public static void Init()
        {
            EventManager.Subscribe(EventType.Animal, EventSubType.Moving, delegate (object obj) { OnAnimalMoving(obj); });
            EventManager.Subscribe(EventType.Colonist, EventSubType.Moving, delegate (object obj) { OnAnimalMoving(obj); });
        }

        public static PlayerActivityType CurrentActivity
        {
            get
            {
                return currentActivity;
            }

            set
            {
                if (currentActivity != value)
                {
                    currentActivity = value;
                    CurrentActivityChanged?.Invoke(null, new EventArgs());
                }
            }
        }

        public static PlayerActivitySubType CurrentActivitySubType
        {
            get
            {
                return currentActivitySubType;
            }

            set
            {
                if (currentActivitySubType != value)
                {
                    currentActivitySubType = value;
                    CurrentActivityChanged?.Invoke(null, new EventArgs());
                }
            }
        }

        public static ThingType? CurrentThingTypeToBuild
        {
            get
            {
                return PlayerActivityBuild.CurrentThingTypeToBuild;
            }

            set
            {
                if (PlayerActivityBuild.CurrentThingTypeToBuild != value)
                {
                    PlayerActivityBuild.CurrentThingTypeToBuild = value;
                    CurrentActivityChanged?.Invoke(null, new EventArgs());
                }
            }
        }

        private static IThing selectedThing;
        public static IThing SelectedThing
        {
            get
            {
                return selectedThing;
            } 
            set
            {
                if (selectedThing == value) return;
                selectedThing = value;
                CurrentActivityChanged?.Invoke(null, new EventArgs());
            }
        }

        public static event EventHandler<EventArgs> CurrentActivityChanged;

        public static void SetCurrentActivity(PlayerActivityType activity, PlayerActivitySubType subType, ThingType? thingType = null, ItemType? itemType = null)
        {
            if (thingType.HasValue && thingType != PlayerActivityBuild.CurrentThingTypeToBuild)
            {
                var direction = ThingTypeManager.GetDefinition(thingType.Value).DefaultBuildDirection;
                PlayerActivityBuild.CurrentDirectionToBuild = direction;
            }

            currentActivity = activity;
            currentActivitySubType = subType;
            PlayerActivityBuild.CurrentThingTypeToBuild = thingType;

            if (itemType.HasValue) PlayerActivityPlaceStackingArea.CurrentItemTypeToBuild = itemType.Value;

            if (activity != PlayerActivityType.Build && activity != PlayerActivityType.PlaceStackingArea)
            {
                BlueprintController.IsVirtualBlueprintBlocked = false;
                BlueprintController.ClearVirtualBlueprint();
            }

            CurrentActivityChanged?.Invoke(null, new EventArgs());
            UI.MouseCursor.Instance.Reset();
            GameScreen.Instance.UpdateHighlights();

            if (activity == PlayerActivityType.Deconstruct && GameScreen.Instance.IsMouseOverNotChildren)
            {
                MouseWorldPosition.Update(scrollPosition, zoom);
                PlayerActivityDeconstruct.Update();
                UI.MouseCursor.Instance.MouseCursorType = MouseCursorType.Recycle;
            }
            else if (activity == PlayerActivityType.Harvest && GameScreen.Instance.IsMouseOverNotChildren)
            {
                MouseWorldPosition.Update(scrollPosition, zoom);
                PlayerActivityHarvest.Update();
                UI.MouseCursor.Instance.MouseCursorType = MouseCursorType.Harvest;
            }
            else if (activity == PlayerActivityType.Geology && GameScreen.Instance.IsMouseOverNotChildren)
            {
                MouseWorldPosition.Update(scrollPosition, zoom);
                PlayerActivityGeology.Update();
                UI.MouseCursor.Instance.MouseCursorType = MouseCursorType.Geology;
            }
            else if (activity == PlayerActivityType.Farm && GameScreen.Instance.IsMouseOverNotChildren)
            {
                MouseWorldPosition.Update(scrollPosition, zoom);
                PlayerActivityFarm.Update();
                UI.MouseCursor.Instance.MouseCursorType = MouseCursorType.Farm;
            }
            else if ((activity == PlayerActivityType.Build || activity == PlayerActivityType.PlaceStackingArea) && GameScreen.Instance.IsMouseOverNotChildren)
            {
                UpdateVirtualBlueprint();
                UI.MouseCursor.Instance.MouseCursorType = MouseCursorType.Hammer;
            }
        }

        public static void Update(Vector2f newScrollPosition, float newZoom)
        {
            scrollPosition = newScrollPosition;
            zoom = newZoom;

            // Update blueprints and cursor labels if amount of metal or stone changed
            if (currentActivity == PlayerActivityType.Build)
            {
                var lander = World.GetThings(ThingType.Lander).FirstOrDefault();
                if (lander != null)
                {
                    var network = World.ResourceNetwork;
                    if (network != null && (network.GetItemTotal(ItemType.Metal) != prevMetal || network.GetItemTotal(ItemType.Stone) != prevStone))
                    {
                        UpdateVirtualBlueprint();
                        prevMetal = network.GetItemTotal(ItemType.Metal);
                        prevStone = network.GetItemTotal(ItemType.Stone);
                    }
                }
            }
            else if (currentActivity == PlayerActivityType.PlaceStackingArea && MouseWorldPosition.Tile != null)
            {
                PlayerActivityPlaceStackingArea.Update();
            }
            else if (currentActivity == PlayerActivityType.Deconstruct && MouseWorldPosition.Tile != null)
            {
                PlayerActivityDeconstruct.Update();
            }
            else if (currentActivity == PlayerActivityType.Harvest && MouseWorldPosition.Tile != null)
            {
                PlayerActivityHarvest.Update();
            }
            else if (currentActivity == PlayerActivityType.Geology && MouseWorldPosition.Tile != null)
            {
                PlayerActivityGeology.Update();
            }
            else if (currentActivity == PlayerActivityType.Farm && MouseWorldPosition.Tile != null)
            {
                PlayerActivityFarm.Update();
            }
            else
            {
                for (int i = 0; i < 5; i++) UI.MouseCursor.Instance.TextLine[i] = "";
            }

            if (UIStatics.CurrentMouseState.LeftButton == ButtonState.Pressed && currentActivity == PlayerActivityType.Build && PlayerActivityBuild.MouseDragStartTile == null)
            {
                PlayerActivityBuild.MouseDragStartTile = PlayerActivityBuild.CurrentDirectionToBuild != Direction.None ? PlayerActivityBuild.BlueprintTargetTile : MouseWorldPosition.Tile;
            }
            else if (UIStatics.CurrentMouseState.LeftButton == ButtonState.Pressed && currentActivity == PlayerActivityType.PlaceStackingArea && PlayerActivityPlaceStackingArea.MouseDragStartTile == null)
            {
                PlayerActivityPlaceStackingArea.MouseDragStartTile = MouseWorldPosition.Tile;
            }
            else if (UIStatics.CurrentMouseState.LeftButton == ButtonState.Released)
            {
                PlayerActivityBuild.MouseDragStartTile = null;
                PlayerActivityPlaceStackingArea.MouseDragStartTile = null;
            }
        }

        public static IThing GetThingUnderCursor(HashSet<ThingType> thingTypes, bool deconstructMode)
        {
            if (UIStatics.Graphics == null) return null;

            if (MouseWorldPosition.Tile != null)
            {
                // Select roof only if visible
                if (GameScreen.Instance.IsRoofVisible && thingTypes.Contains(ThingType.Roof))
                {
                    var roof = MouseWorldPosition.Tile.ThingsPrimary.FirstOrDefault(t => t.ThingType == ThingType.Roof);
                    if (roof != null) return roof;
                }

                // Find colonist
                if (!deconstructMode && thingTypes.Contains(ThingType.Colonist))
                {
                    var colonist = World.GetThings<IColonist>(ThingType.Colonist).FirstOrDefault(c => (c.Position - MouseWorldPosition.TerrainPosition).Length() < 0.33f);
                    if (colonist != null) return colonist;
                }

                // Select wall or door if clicked SE or SW edge of tile
                if (MouseWorldPosition.IsEdge && (MouseWorldPosition.ClosestEdge == Direction.SE || MouseWorldPosition.ClosestEdge == Direction.SW))
                {
                    var wall = MouseWorldPosition.Tile.ThingsPrimary.OfType<IWall>().FirstOrDefault(w => w.Direction == MouseWorldPosition.ClosestEdge);
                    if (wall != null && thingTypes.Contains(wall.ThingType)) return wall;
                }

                // Select adjacent wall or door if clicked NE or NW edge of tile
                if (MouseWorldPosition.IsEdge 
                    && (MouseWorldPosition.ClosestEdge == Direction.NW || MouseWorldPosition.ClosestEdge == Direction.NE) 
                    && MouseWorldPosition.Tile.GetTileToDirection(MouseWorldPosition.ClosestEdge) is ISmallTile t2)
                {
                    if (thingTypes.Contains(ThingType.Wall)
                        && t2.ThingsPrimary.FirstOrDefault(t => t.ThingType == ThingType.Wall && (t as IRotatableThing)?.Direction == DirectionHelper.Reverse(MouseWorldPosition.ClosestEdge)) is IThing wall)
                    {
                        return wall;
                    }

                    if (thingTypes.Contains(ThingType.Door)
                         && t2.ThingsPrimary.FirstOrDefault(t => t.ThingType == ThingType.Door && (t as IRotatableThing)?.Direction == DirectionHelper.Reverse(MouseWorldPosition.ClosestEdge)) is IThing door)
                    {
                        return door;
                    }
                }

                // Stacking area takes priority
                var stackingArea = MouseWorldPosition.Tile.ThingsPrimary.OfType<IStackingArea>().FirstOrDefault();
                if (stackingArea != null) return stackingArea;

                var result = MouseWorldPosition.Tile.ThingsAll.OrderBy(t => GetSelectPriority(t.ThingType, deconstructMode))
                    .FirstOrDefault(t => t.ThingType != ThingType.Roof && (((deconstructMode && (t as IColonist)?.IsDead == true)) || t.ThingType != ThingType.Colonist) && thingTypes.Contains(t.ThingType));

                // If we found nothing, look for other walls to the NE or NW that border this tile
                if (result == null && thingTypes.Contains(ThingType.Wall)) result = MouseWorldPosition.Tile.TileToNW?.ThingsPrimary?.FirstOrDefault(t => t.ThingType == ThingType.Wall && (t as IRotatableThing)?.Direction == Direction.SE);
                if (result == null && thingTypes.Contains(ThingType.Door)) result = MouseWorldPosition.Tile.TileToNW?.ThingsPrimary?.FirstOrDefault(t => t.ThingType == ThingType.Door && (t as IRotatableThing)?.Direction == Direction.SE);
                if (result == null && thingTypes.Contains(ThingType.Wall)) result = MouseWorldPosition.Tile.TileToNE?.ThingsPrimary?.FirstOrDefault(t => t.ThingType == ThingType.Wall && (t as IRotatableThing)?.Direction == Direction.SW);
                if (result == null && thingTypes.Contains(ThingType.Door)) result = MouseWorldPosition.Tile.TileToNE?.ThingsPrimary?.FirstOrDefault(t => t.ThingType == ThingType.Door && (t as IRotatableThing)?.Direction == Direction.SW);

                return result;
            }

            return null;
        }

        public static void OnMouseMoved()
        {
            if (UIStatics.Graphics == null || ModalBackgroundBox.Instance.IsInteractive) return;

            if (MouseManager.CurrentMouseOverElement == GameScreen.Instance)
            {
                if (MouseWorldPosition.Update(scrollPosition, zoom) && currentActivity.In(PlayerActivityType.Build, PlayerActivityType.PlaceStackingArea))
                {
                    UpdateVirtualBlueprint();
                    GameScreen.Instance.UpdateHighlights();
                }
            }
            else if (MouseWorldPosition.Tile != null)
            {
                MouseWorldPosition.Tile = null;
                UpdateVirtualBlueprint();
            }
        }

        private static int GetSelectPriority(ThingType thingType, bool deconstructMode)
        {
            if (thingType == ThingType.Rocket || thingType == ThingType.Roof) return 0;
            if (thingType == ThingType.Colonist && !deconstructMode) return 1;
            if (thingType == ThingType.RocketGantry || thingType == ThingType.EnvironmentControl) return 2;
            if (thingType == ThingType.Colonist) return 4;
            if (thingType.IsFoundationLayer()) return 5;
            if (thingType == ThingType.ConduitNode) return 6;
            if (thingType == ThingType.Wall || thingType == ThingType.Door) return 7;
            return 3;
        }

        public static void UpdateVirtualBlueprint()
        {
            if (CurrentActivity == PlayerActivityType.Build)
            {
                PlayerActivityBuild.Update();
            }
            else if (CurrentActivity == PlayerActivityType.PlaceStackingArea)
            {
                PlayerActivityPlaceStackingArea.Update();
            }
            else if (World.VirtualBlueprint.Any() && UIStatics.CurrentMouseState.LeftButton == ButtonState.Released)
            {
                BlueprintController.ClearVirtualBlueprint();
            }
        }

        public static void Build()
        {
            var lander = World.GetThings(ThingType.Lander).First() as Lander;
            var blueprints = BlueprintController.ConvertVirtualBuildingToBlueprint(lander);
            if (blueprints.Any() && !KeyboardManager.IsShift)
            {
                SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
            }
        }

        public static void CreateResourceStack()
        {
            if (BlueprintController.ConvertVirtualResourceStackToBlueprint() && !KeyboardManager.IsShift)
            {
                SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
            }
        }

        public static void RotateBlueprintLeft()
        {
            PlayerActivityBuild.CurrentDirectionToBuild = DirectionHelper.AntiClockwise90(PlayerActivityBuild.CurrentDirectionToBuild);
            UpdateVirtualBlueprint();
        }

        public static void RotateBlueprintRight()
        {
            PlayerActivityBuild.CurrentDirectionToBuild = DirectionHelper.Clockwise90(PlayerActivityBuild.CurrentDirectionToBuild);
            UpdateVirtualBlueprint();
        }

        private static void OnAnimalMoving(object obj)
        {
            if (currentActivity == PlayerActivityType.Build)
            {
                if (obj is IAnimal animal && World.VirtualBlueprint.Any(b => b.MainTileIndex == animal.MainTileIndex || b.MainTile.Index == animal.PrevTileIndex))
                {
                    UpdateVirtualBlueprint();
                }
            }
        }
    }
}
