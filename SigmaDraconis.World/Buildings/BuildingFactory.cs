namespace SigmaDraconis.World.Buildings
{
    using Shared;
    using WorldInterfaces;

    public static class BuildingFactory
    {
        public static Building Get(ThingType thingType, ISmallTile mainTile, int size = 1, Direction direction = Direction.SE)
        {
            switch (thingType)
            {
                case ThingType.ConduitNode:
                    return new ConduitNode(mainTile);
                case ThingType.ConduitMajor:
                    return new ConduitMajor(mainTile);
                case ThingType.ConduitMinor:
                    return new ConduitMinor(mainTile);
                case ThingType.CharcoalMaker:
                    return new CharcoalMaker(mainTile);
                case ThingType.StoneFurnace:
                    return new StoneFurnace(mainTile, direction);
                case ThingType.DirectionalHeater:
                    return new DirectionalHeater(mainTile, direction);
                case ThingType.WindTurbine:
                    return new WindTurbine(mainTile);
                case ThingType.SolarPanelArray:
                    return new SolarPanel(mainTile);
                case ThingType.Mine:
                    return new Mine(mainTile);
                case ThingType.ElectricFurnace:
                    return new ElectricFurnace(mainTile, direction);
                case ThingType.BatteryCellFactory:
                    return new BatteryCellFactory(mainTile, direction);
                case ThingType.CompositesFactory:
                    return new CompositesFactory(mainTile, direction);
                case ThingType.CompostFactory:
                    return new CompostFactory(mainTile, direction);
                case ThingType.GlassFactory:
                    return new GlassFactory(mainTile, direction);
                case ThingType.KekFactory:
                    return new KekFactory(mainTile, direction);
                case ThingType.SolarCellFactory:
                    return new SolarCellFactory(mainTile, direction);
                case ThingType.Battery:
                    return new Battery(mainTile);
                case ThingType.LaunchPad:
                    return new LaunchPad(mainTile);
                case ThingType.RocketGantry:
                    return new RocketGantry(mainTile);
                case ThingType.Rocket:
                    return new Rocket(mainTile);
                case ThingType.HydrogenStorage:
                    return new HydrogenStorage(mainTile);
                case ThingType.ItemsStorage:
                    return new ItemsStorage(mainTile);
                case ThingType.Silo:
                    return new Silo(mainTile);
                case ThingType.Generator:
                    return new Generator(mainTile, direction);
                case ThingType.BiomassPower:
                    return new BiomassPower(mainTile, direction);
                case ThingType.CoalPower:
                    return new CoalPower(mainTile, direction);
                case ThingType.FuelFactory:
                    return new FuelFactory(mainTile, direction);
                case ThingType.HydrogenBurner:
                    return new HydrogenBurner(mainTile);
                case ThingType.ResourceProcessor:
                    return new ResourceProcessor(mainTile);
                case ThingType.Lamp:
                    return new Lamp(mainTile);
                case ThingType.FoodDispenser:
                    return new FoodDispenser(mainTile);
                case ThingType.FoodStorage:
                    return new FoodStorage(mainTile);
                case ThingType.KekDispenser:
                    return new KekDispenser(mainTile);
                case ThingType.MushFactory:
                    return new MushFactory(mainTile);
                case ThingType.FoundationMetal:
                case ThingType.FoundationStone:
                    return new Foundation(mainTile, thingType);
                case ThingType.Wall:
                    return new Wall(mainTile, direction);
                case ThingType.Door:
                    return new Door(mainTile, direction);
                case ThingType.Roof:
                    return new Roof(mainTile);
                case ThingType.EnvironmentControl:
                    return new EnvironmentControl(mainTile);
                case ThingType.Cooker:
                    return new Cooker(mainTile);
                case ThingType.PlanterHydroponics:
                case ThingType.PlanterStone:
                    return new Planter(mainTile, thingType);
                case ThingType.AlgaePool:
                    return new AlgaePool(mainTile);
                case ThingType.Biolab:
                case ThingType.GeologyLab:
                case ThingType.MaterialsLab:
                    return new Lab(mainTile, direction, thingType);
                case ThingType.OreScanner:
                    return new OreScanner(mainTile);
                case ThingType.SleepPod:
                    return new SleepPod(mainTile, direction);
                case ThingType.LandingPod:
                    return new LandingPod(mainTile);
                case ThingType.TableMetal:
                case ThingType.TableStone:
                    return new Table(mainTile, thingType);
                case ThingType.WaterDispenser:
                    return new WaterDispenser(mainTile);
                case ThingType.WaterPump:
                    return new WaterPump(mainTile, direction);
                case ThingType.ShorePump:
                    return new ShorePump(mainTile, direction);
                case ThingType.WaterStorage:
                    return new WaterStorage(mainTile);
                default:
                    return new Building(thingType, mainTile, size);
            }
        }
    }
}
