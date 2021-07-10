namespace SigmaDraconis.WorldControllers
{
    using Draconis.Shared;
    using Config;
    using Shared;
    using System.Collections.Generic;
    using System.Linq;
    using World;
    using World.Blueprints;
    using World.Buildings;
    using World.ResourceStacks;
    using World.Rooms;
    using WorldInterfaces;

    public static class BlueprintController
    {
        public static bool IsVirtualBlueprintBlocked { get; set; }

        public static void Reset()
        {
            World.ClearVirtualBlueprint();
            IsVirtualBlueprintBlocked = false;
            World.ConfirmedBlueprints.Clear();
            World.RecycleBlueprints.Clear();
        }

        public static void ClearVirtualBlueprint()
        {
            IsVirtualBlueprintBlocked = false;
            var clone = World.VirtualBlueprint.ToList();
            World.ClearVirtualBlueprint();

            foreach (var section in clone)
            {
                EventManager.RaiseEvent(EventType.VirtualBlueprint, EventSubType.Removed, section);
            }
        }

        public static bool AddVirtualBuilding(ISmallTile tile, ThingType thingType, bool canBuild = true, int animationFrame = 1, Direction direction = Direction.SE)
        {
            // If the building exists, return true but don't add the blueprint... probably we are adding multiple, such as walls, and need to fill the gaps.
            if (thingType != ThingType.StackingArea && BuildHelper.BuildingExists(tile, thingType, direction))
            {
                return true;
            }

            var definition = ThingTypeManager.GetDefinition(thingType);
            var building = new Blueprint(thingType, tile, definition.Size.X, canBuild) { AnimationFrame = animationFrame, Direction = direction };

            World.VirtualBlueprint.Add(building);
            if (canBuild && building.ThingType == ThingType.ConduitNode)
            {
                // Highlight expanded buildable area
                World.SetVirtualBuildableArea(building.MainTile);
            }

            return canBuild;
        }

        /// <summary>
        /// Returns true on success
        /// </summary>
        /// <returns></returns>
        public static bool ConvertVirtualResourceStackToBlueprint()
        {
            if (World.VirtualBlueprint.Count != 1)
            {
                ClearVirtualBlueprint();
                return false;
            }

            // Can create resource stack as long as there is nothing other than conduits, foundations, walls, doors and roofs in the tile
            var blueprint = World.VirtualBlueprint[0];
            var tile = blueprint.MainTile;
            if (tile.TerrainType != TerrainType.Dirt
                || tile.ThingsAll.Any(t => !t.ThingType.IsConduit() && !t.ThingType.IsFoundation() && !t.ThingType.In(ThingType.Wall, ThingType.Door, ThingType.Roof)))
            {
                ClearVirtualBlueprint();
                return false;
            }

            // Sanity check
            if (!Constants.ResourceStackTypes.ContainsValue(blueprint.ThingType))
            {
                ClearVirtualBlueprint();
                return false;
            }

            // Convert the blueprint
            blueprint.ColourR = 0f;
            blueprint.ColourG = 0f;
            blueprint.ColourB = 1f;
            blueprint.ColourA = 0.2f;
            World.ConfirmedBlueprints.Add(blueprint.Id, blueprint);
            EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Added, blueprint);

            // Create the building
            var building = new ResourceStack(blueprint.ThingType, blueprint.MainTile, 0)
            {
                TargetItemCount = blueprint.AnimationFrame,
                RenderAlpha = 0f,
                ShadowAlpha = 0f,
                HaulPriority = WorkPriority.Normal,
                IsReady = true
            };

            World.AddThing(building);

            ClearVirtualBlueprint();
            return true;
        }

        public static List<Blueprint> ConvertVirtualBuildingToBlueprint(Lander lander)
        {
            var network = World.ResourceNetwork;

            // Special rule for converting completed wall to door
            if (World.VirtualBlueprint.Count == 1 && World.VirtualBlueprint[0].CanBuild && World.VirtualBlueprint[0].ThingType == ThingType.Door
                && World.VirtualBlueprint[0].MainTile.ThingsPrimary.Any(t => t.ThingType == ThingType.Wall && t is IWall w && w.IsReady && w.Direction == World.VirtualBlueprint[0].Direction)
                && network?.CanTakeEnergy(ThingTypeManager.GetEnergyCost(ThingType.Door)) == true)
            {
                var tile = World.VirtualBlueprint[0].MainTile;
                network.TakeEnergy(ThingTypeManager.GetEnergyCost(ThingType.Door));

                // Remove the wall
                var wall = tile.ThingsPrimary.First(t => t is Wall w && w.Direction == World.VirtualBlueprint[0].Direction) as Wall;
                World.RemoveThing(wall);  // Can update the blueprint, so careful here

                // Add the door
                var door = new Door(tile, wall.ConstructionProgress, wall.Direction);
                World.AddThing(door);
                door.AfterAddedToWorld();
                door.AfterConstructionComplete();
                ClearVirtualBlueprint();
                return new List<Blueprint>();
            }

            // Special rule for converting completed door to wall
            if (World.VirtualBlueprint.Count == 1 && World.VirtualBlueprint[0].CanBuild && World.VirtualBlueprint[0].ThingType == ThingType.Wall
                && World.VirtualBlueprint[0].MainTile.ThingsPrimary.Any(t => t is IDoor d && d.IsReady && d.Direction == World.VirtualBlueprint[0].Direction)
                && network?.CanTakeEnergy(ThingTypeManager.GetEnergyCost(ThingType.Wall)) == true)
            {
                var tile = World.VirtualBlueprint[0].MainTile;
                network.TakeEnergy(ThingTypeManager.GetEnergyCost(ThingType.Wall));

                // Remove the door
                var door = tile.ThingsPrimary.First(t => t is IDoor d && d.Direction == World.VirtualBlueprint[0].Direction) as IDoor;
                World.RemoveThing(door);  // Can update the blueprint, so careful here

                // Add the wall
                var wall = BuildingFactory.Get(ThingType.Wall, tile, 1, door.Direction);
                wall.AnimationFrame = 1;
                wall.RenderAlpha = 1f;
                wall.ShadowAlpha = 1f;
                World.AddThing(wall);
                wall.AfterAddedToWorld();
                wall.ConstructionProgress = 100;
                wall.AfterConstructionComplete();
                ClearVirtualBlueprint();
                return new List<Blueprint>();
            }

            var buildingBlueprints = new List<Blueprint>();

            var resourceCosts = GetResourcesRequiredForVirtualBuildings();
            Energy totalEnergyCost = GetEnergyRequiredForVirtualBuildings();

            if (World.VirtualBlueprint.Any(b => !b.CanBuild) || network == null || !network.CanTakeEnergy(totalEnergyCost))
            {
                return new List<Blueprint>();
            }

            foreach (var kv in resourceCosts)
            {
                if (kv.Value > 0 && !network.CanTakeItems(lander, kv.Key, kv.Value)) return new List<Blueprint>();
            }

            if (World.VirtualBlueprint.Count == 1 && World.VirtualBlueprint[0].ThingType == ThingType.Door)
            {
                // Special cases for converting wall to door
                var wall = World.VirtualBlueprint[0];
                var blueprint = World.ConfirmedBlueprints.Values.FirstOrDefault(cb => cb.ThingType == ThingType.Wall && cb.MainTile == wall.MainTile && cb.Direction == wall.Direction);
                if (blueprint != null)
                {
                    var oldBuilding = blueprint.MainTile.ThingsPrimary.FirstOrDefault(t => t.ThingType == ThingType.Wall && (t as IWall).Direction == blueprint.Direction);
                    if (oldBuilding != null)
                    {
                        blueprint.ChangeType(ThingType.Door);

                        // New building
                        var newBuilding = BuildingFactory.Get(ThingType.Door, blueprint.MainTile, 1, blueprint.Direction);
                        newBuilding.RenderAlpha = oldBuilding.RenderAlpha;
                        newBuilding.ShadowAlpha = oldBuilding.ShadowAlpha;
                        World.AddThing(newBuilding);
                        newBuilding.AfterAddedToWorld();

                        // Remove old building
                        World.RemoveThing(oldBuilding);
                        EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Updated, blueprint);
                    }

                    network.TakeEnergy(totalEnergyCost);
                    ClearVirtualBlueprint();

                    return new List<Blueprint> { blueprint };
                }
            }
            else if (World.VirtualBlueprint.Count == 1 && World.VirtualBlueprint[0].ThingType == ThingType.Wall)
            {
                // Special cases for converting door to wall
                var wall = World.VirtualBlueprint[0];
                var blueprint = World.ConfirmedBlueprints.Values.FirstOrDefault(cb => cb.ThingType == ThingType.Door && cb.MainTile == wall.MainTile && cb.Direction == wall.Direction);
                if (blueprint != null)
                {
                    var oldBuilding = blueprint.MainTile.ThingsPrimary.FirstOrDefault(t => t.ThingType == ThingType.Door && (t as IWall).Direction == blueprint.Direction);
                    if (oldBuilding != null)
                    {
                        blueprint.ChangeType(ThingType.Wall);

                        // New building
                        var newBuilding = BuildingFactory.Get(ThingType.Wall, blueprint.MainTile, 1, blueprint.Direction);
                        newBuilding.RenderAlpha = oldBuilding.RenderAlpha;
                        newBuilding.ShadowAlpha = oldBuilding.ShadowAlpha;
                        World.AddThing(newBuilding);
                        newBuilding.AfterAddedToWorld();

                        // Remove old building
                        World.RemoveThing(oldBuilding);
                        EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Updated, blueprint);
                    }

                    network.TakeEnergy(totalEnergyCost);
                    ClearVirtualBlueprint();

                    return new List<Blueprint> { blueprint };
                }
            }

            var roofs = new List<IBuildableThing>();
            foreach (var blueprint in World.VirtualBlueprint.ToList())
            {
                var building = ConvertBlueprint(blueprint, buildingBlueprints);
                if (building is Roof r) roofs.Add(r);
                else if (blueprint.ThingType == ThingType.ConduitNode)
                {
                    ConduitHelper.AddMajorConduits(building as IConduitNode);
                }
                else if (ThingTypeManager.GetDefinition(blueprint.ThingType).ConduitType == ThingType.ConduitMinor
                    && blueprint.MainTile.ThingsPrimary.All(t => !t.ThingType.IsConduit() || t == building))
                {
                    foreach (var tile in blueprint.AllTiles) ConduitHelper.AddMinorConduit(tile, true);
                }

                World.VirtualBlueprint.Remove(blueprint);
                EventManager.RaiseEvent(EventType.VirtualBlueprint, EventSubType.Removed, blueprint);
            }

            if (roofs.Any()) RoomManager.CreateRoom(roofs);

            if (buildingBlueprints.Any())
            {
                network.TakeEnergy(totalEnergyCost);
                WorldStats.Increment(WorldStatKeys.EnergyUsed, totalEnergyCost.Joules);

                foreach (var kv in resourceCosts.Where(x => x.Value > 0))
                {
                    network.TakeItems(lander, kv.Key, kv.Value);
                    if (kv.Key == ItemType.Metal) WorldStats.Increment(WorldStatKeys.MetalUsed, kv.Value);
                    else if (kv.Key == ItemType.Stone) WorldStats.Increment(WorldStatKeys.StoneUsed, kv.Value);
                }
            }

            ClearVirtualBlueprint();
            return buildingBlueprints;
        }

        private static Building ConvertBlueprint(Blueprint blueprint, List<Blueprint> buildingBlueprints)
        {
            var building = CreateBuilding(blueprint);
            if (World.Prefabs.Count(blueprint.ThingType) > 0)
            {
                building.IsConstructedFromPrefab = true;
                World.Prefabs.Remove(blueprint.ThingType);
            }

            blueprint.ColourR = 0f;
            blueprint.ColourG = 0f;
            blueprint.ColourB = 1f;
            blueprint.ColourA = 0.2f;
            buildingBlueprints.Add(blueprint);
            World.ConfirmedBlueprints.Add(blueprint.Id, blueprint);
            EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Added, blueprint);
            return building;
        }

        private static Building CreateBuilding(Blueprint blueprint)
        {
            var building = BuildingFactory.Get(blueprint.ThingType, blueprint.MainTile, blueprint.Definition.Size.X, blueprint.Direction);
            if (building.ThingType != ThingType.SolarPanelArray && building.ThingType != ThingType.WindTurbine) building.AnimationFrame = blueprint.AnimationFrame;
            building.RenderAlpha = 0f;
            building.ShadowAlpha = 0f;
            World.AddThing(building);
            building.AfterAddedToWorld();
            return building;
        }

        public static Energy GetEnergyRequiredForVirtualBuildings()
        {
            if (World.VirtualBlueprint.Count == 1)
            {
                var vb = World.VirtualBlueprint[0];
                var wallCost = ThingTypeManager.GetEnergyCost(ThingType.Wall);
                var doorCost = ThingTypeManager.GetEnergyCost(ThingType.Door);

                if (vb.ThingType == ThingType.Door && World.ConfirmedBlueprints.Values.Any(t => t.ThingType == ThingType.Wall && t.MainTileIndex == vb.MainTileIndex && t.Direction == vb.Direction))
                {
                    return doorCost > wallCost ? doorCost - wallCost : 0;
                }

                if (vb.ThingType == ThingType.Wall && World.ConfirmedBlueprints.Values.Any(t => t.ThingType == ThingType.Door && t.MainTileIndex == vb.MainTileIndex && t.Direction == vb.Direction))
                {
                    return wallCost > doorCost ? wallCost - doorCost : 0;
                }
            }

            Energy energyRequired = World.VirtualBlueprint.Where(vb => World.Prefabs.Count(vb.ThingType) == 0).Sum(b => ThingTypeManager.GetEnergyCost(b.ThingType));
            return energyRequired;
        }

        public static Dictionary<ItemType, int> GetResourcesRequiredForVirtualBuildings()
        {
            var result = new Dictionary<ItemType, int> { { ItemType.Metal, 0 }, { ItemType.Stone, 0 }, { ItemType.BatteryCells, 0 }, { ItemType.Composites, 0 }, { ItemType.SolarCells, 0 }, { ItemType.Glass, 0 }, { ItemType.Compost, 0 } };

            foreach (var blueprint in World.VirtualBlueprint)
            {
                if (World.Prefabs.Count(blueprint.ThingType) > 0) continue;

                var vb0 = World.VirtualBlueprint.Count == 1 ? World.VirtualBlueprint[0] : null;
                if (vb0 != null && vb0.ThingType == ThingType.Door && World.VirtualBlueprint[0].MainTile.ThingsPrimary.Any(t => t.ThingType == ThingType.Wall && (t as IRotatableThing)?.Direction == vb0.Direction))
                {
                    // Doors are free when they are replacing a wall
                    continue;
                }

                if (vb0 != null && vb0.ThingType == ThingType.Wall && World.VirtualBlueprint[0].MainTile.ThingsPrimary.Any(t => t.ThingType == ThingType.Door && (t as IRotatableThing)?.Direction == vb0.Direction))
                {
                    // Walls are free when they are replacing a door
                    continue;
                }

                foreach (var kv in blueprint.Definition.ConstructionCosts) result[kv.Key] += kv.Value;
            }

            return result;
        }
    }
}
