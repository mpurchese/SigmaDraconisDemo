namespace SigmaDraconis.WorldGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.Buildings;
    using World.Fauna;
    using World.Flora;
    using World.Rocks;
    using World.Prefabs;
    using WorldControllers;
    using WorldInterfaces;

    internal class WorldCreator
    {
        public static void Create(WorldTemplate template, int startTileIndex)
        {
            InitTerrain(template);
            AddRocks(template);
            AddPlants(template);

            AddAnimals();

            World.UpdateBigTileRenderLists();
            PlantGrowthController.SortSeedQueue();

            CreateLandingZone(startTileIndex);

            // Free stuff
            World.Prefabs = new PrefabCollection();
            World.Prefabs.Add(ThingType.ResourceProcessor);
            World.Prefabs.Add(ThingType.WaterPump);

            World.InitialGameVersion = GameVersion.CurrentGameVersion;
        }

        private static void InitTerrain(WorldTemplate template)
        {
            for (int i = 0; i < template.BigTiles.Count; i++)
            {
                var source = template.BigTiles[i];
                var target = World.BigTiles[i];

                target.BigTileTextureIdentifier = source.BigTileTextureIdentifier;
                target.TerrainType = source.TerrainType;
                target.UpdateCoords();
            }

            for (int i = 0; i < template.SmallTiles.Count; i++)
            {
                var source = template.GetSmallTile(i);
                var target = World.GetSmallTile(i);

                target.BiomeType = source.BiomeType;
                target.GroundCoverDensity = source.GroundCoverDensity;
                target.GroundCoverDirection = source.GroundCoverDirection;
                target.GroundCoverMaxDensity = source.GroundCoverMaxDensity;
                target.TerrainType = source.TerrainType;
                target.SetResources(source.GetResources());
            }
        }

        private static void AddRocks(WorldTemplate template)
        {
            foreach (var rock in template.GetRocks())
            {
                var tile = World.GetSmallTile(rock.MainTileIndex);
                World.AddThing(new Rock(tile, rock.ThingType, rock.ResourceType));
            }
        }

        private static void AddPlants(WorldTemplate template)
        {
            var random = new Random();
            foreach (var plant in template.GetPlants())
            {
                var tile = World.GetSmallTile(plant.MainTileIndex);
                switch (plant.ThingType)
                {
                    case ThingType.Bush: World.AddThing(new Bush(tile)); break;
                    case ThingType.CoastGrass:
                        var offset = CoastGrass.GetPositionOnTileForNewPlant(tile);
                        if (offset != null) World.AddThing(new CoastGrass(tile, offset, Rand.NextFloat()));
                        break;
                    case ThingType.Grass: World.AddThing(new Swordleaf(tile, Rand.NextFloat())); break;
                    case ThingType.SmallPlant2: World.AddThing(new SmallPlant2(tile) { GrowthPercent = random.Next(30) + 40 }); break;
                    case ThingType.SmallPlant3: World.AddThing(new SmallPlant3(tile, false)); break;
                    case ThingType.SmallPlant4: World.AddThing(new SmallPlant4(tile)); break;
                    case ThingType.SmallPlant5: World.AddThing(new SmallPlant5(tile)); break;
                    case ThingType.SmallPlant6: World.AddThing(new SmallPlant6(tile)); break;
                    case ThingType.SmallPlant7: World.AddThing(new SmallPlant7(tile)); break;
                    case ThingType.SmallPlant8: World.AddThing(new SmallPlant8(tile)); break;
                    case ThingType.SmallPlant9: World.AddThing(new SmallPlant9(tile)); break;
                    case ThingType.SmallPlant10: World.AddThing(new SmallPlant10(tile)); break;
                    case ThingType.SmallPlant11: World.AddThing(new SmallPlant11(tile)); break;
                    case ThingType.SmallPlant12: World.AddThing(new SmallPlant12(tile)); break;
                    case ThingType.SmallPlant13: World.AddThing(new SmallPlant13(tile)); break;
                    case ThingType.BigSpineBush: World.AddThing(new BigSpineBush(tile)); break;
                    case ThingType.SmallSpineBush: World.AddThing(new SmallSpineBush(tile)); break;
                    case ThingType.Tree: World.AddThing(new Tree(tile, (float)random.NextDouble() * 40f)); break;
                }
            }
        }

        private static void CreateLandingZone(int tileIndex)
        {
            var startTile = World.GetSmallTile(tileIndex);

            // Clear a bit of space and reveal mining resources
            foreach (var tile in startTile.AdjacentTiles8.SelectMany(x => x.AdjacentTiles4).SelectMany(x => x.AdjacentTiles4).Distinct())
            {
                foreach (var thing in tile.ThingsAll.ToList())
                {
                    // If thing was a bush then remove any bugs
                    if (thing.ThingType == ThingType.Bush || thing.ThingType == ThingType.SmallPlant4)
                    {
                        foreach (var bug in thing.AllTiles.SelectMany(t => t.ThingsAll).OfType<Bug>().ToList())
                        {
                            World.RemoveThing(bug);
                        }
                    }

                    World.RemoveThing(thing);
                }

                foreach (var tile2 in tile.AdjacentTiles8) tile2.SetIsMineResourceVisible(true, false);
            }

            // Remove any trees to the south, so they don't obscure view of the lander
            var tiles = new List<ISmallTile>(5)
            {
                startTile.TileToW.TileToW, startTile.TileToW.TileToSW, startTile.TileToW, startTile.TileToSW, startTile, startTile.TileToSE, startTile.TileToE, startTile.TileToE.TileToSE, startTile.TileToE,  startTile.TileToE.TileToE
            };

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    tiles[j] = tiles[j]?.TileToS;
                    if (tiles[j] != null)
                    {
                        foreach (var tree in tiles[j].ThingsPrimary.OfType<Tree>().ToList())
                        {
                            World.RemoveThing(tree);
                        }
                    }
                }
            }

            var lander = new Lander(startTile);
            World.AddThing(lander);
            lander.ChargeLevel = Energy.FromKwH(Constants.LanderEnergyStorage);
            lander.AfterAddedToWorld();
            lander.AfterConstructionComplete();

            World.ResourceNetwork.UpdateStartOfFrame(true);
            World.AddFood(0, 2);
            World.ResourceNetwork.AddItem(ItemType.Food);
            World.ResourceNetwork.AddItem(ItemType.Food);

            World.AddThing(new LanderPanel(startTile.TileToNW, 1));
            World.AddThing(new LanderPanel(startTile.TileToNE, 2));
            World.AddThing(new LanderPanel(startTile.TileToSW, 3));
            World.AddThing(new LanderPanel(startTile.TileToSE, 4));
        }

        private static void AddAnimals()
        {
            var random = new Random();
            var tileCount = World.SmallTiles.Count;
            var count = 0;
            while (count < 20)
            {
                var tileIndex = random.Next(tileCount);
                var tile = World.GetSmallTile(tileIndex);
                if (tile.TerrainType == TerrainType.Dirt && !tile.ThingsAll.Any())
                {
                    var tortoise = new Tortoise(tile) { Rotation = (random.Next(4) + 0.5f) * Mathf.PI * 0.5f };
                    World.AddThing(tortoise);
                    tortoise.Init();
                    count++;
                }
            }

            foreach (var bush in World.GetThings<IAnimatedThing>(ThingType.Bush).Where(b => b.AnimationFrame > 4))
            {
                var bug = new RedBug(bush.AllTiles[random.Next(4)]);
                World.AddThing(bug);
                bug.Init();
            }

            foreach (var plant in World.GetThings<IAnimatedThing>(ThingType.SmallPlant4).Where(p => p.AnimationFrame >= 8 && random.Next(6) == 0))
            {
                var bug = new BlueBug(plant.MainTile);
                World.AddThing(bug);
                bug.Init();
            }
        }
    }
}
